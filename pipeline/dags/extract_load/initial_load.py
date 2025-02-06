from dotenv import load_dotenv
import os
from datetime import date
from datetime import datetime
from datetime import timedelta
import pandas as pd
import logging
import json

logger = logging.getLogger(__name__)
path=os.getcwd()+'/dags/.env'
load_dotenv(path)

from airflow import DAG

from airflow.decorators import dag, task
from airflow.providers.google.cloud.hooks.gcs import GCSHook
from airflow.providers.google.cloud.transfers.local_to_gcs import LocalFilesystemToGCSOperator
from airflow.contrib.operators import gcs_to_bq
from airflow.providers.postgres.hooks.postgres import PostgresHook
from airflow.providers.postgres.operators.postgres import PostgresOperator
from airflow.operators.python import PythonOperator
from airflow.operators.bash import BashOperator
from airflow.exceptions import AirflowSkipException

TMP_DATA_PATH = os.getcwd()+'/data'
TABLES_LIST_PATH = os.getcwd()+'/dags/dvd_rental/dict/tables.json'

postgres_conn_id = "postgres_dvd"
gcp_conn_id = "gcp_dvd"
temp_dataset = "temp_table"

SEED_FILL_YEAR = 2023
SEED_START_DATE = datetime(1970, 1, 1)
SEED_END_DATE = datetime(SEED_FILL_YEAR, 12, 20)
SEED_END_DATE_NODASH = SEED_END_DATE.strftime("%Y%m%d")


@dag(
    "SEED.dvdrental_pipeline_01",
    default_args={
        'email': ['youremail@gmail.com'],
        'email_on_failure': True,
        'email_on_retry': False
    },
    schedule_interval='@once',
    start_date=datetime(2023, 12, 20),
    tags=["dvdrental_pipeline"],
    catchup=False,
    max_active_tasks=30,
    max_active_runs=3
)
def el_pipeline():

    if(os.path.exists(TABLES_LIST_PATH)):
        with open(TABLES_LIST_PATH, 'r') as json_file:
            tables = json.load(json_file)
    else:
        logging.info(f"Company Code JSON file not found: {TABLES_LIST_PATH}")

    for table in tables:

        @task(
            task_id=f"extract_{table['table_name']}_data",
        )
        def extract_postgres(table, postgres_conn_id, **kwargs):

            sql_query = f"""
                        SELECT * FROM {table['table_name']}
                        WHERE {table['watermark_column']} >= CAST('{SEED_START_DATE}' AS DATE)
                        AND {table['watermark_column']} < CAST('{SEED_END_DATE}' AS DATE)
                        """
            
            logging.info(f"Executing query: {sql_query}")

            try:
                output_fp = f"{TMP_DATA_PATH}/{table['table_name']}_{SEED_END_DATE_NODASH}.parquet"
                pg_hook = PostgresHook(postgres_conn_id=postgres_conn_id)
                df = pg_hook.get_pandas_df(sql_query)
                logging.info(f"Writing {len(df)} rows to {output_fp}")
                df.to_parquet(output_fp, index=False, compression="snappy")
                logging.info(f"Successfully saved file to parquet: {output_fp}")
            except Exception as e:
                logging.error(e)
                if("Invalid object name" in str(e)):
                    raise AirflowSkipException
                else:
                    raise e
        

        @task( 
            task_id=f"load_{table['table_name']}_to_gcs",
        )
        def load_to_gcs(table, gcp_conn_id, **kwargs):
            
            try:
                gcp_hook = GCSHook(gcp_conn_id=gcp_conn_id)
                gcs_bucket = "dvdrental_project"
                gcs_object = f"{table['table_name']}/{table['table_name']}_{SEED_END_DATE_NODASH}.parquet"
                gcp_hook.upload(
                    bucket_name=gcs_bucket,
                    object_name=gcs_object,
                    filename=f"{TMP_DATA_PATH}/{table['table_name']}_{SEED_END_DATE_NODASH}.parquet",
                )
                logging.info(f"Successfully uploaded file to GCS: {gcs_object}")
            except Exception as e:
                logging.error(e)
                raise e

        
        @task(
            task_id=f"create_temp_{table['table_name']}_table_in_bq",
        )
        def create_temp_table_in_bq(table, gcp_conn_id, **kwargs):
                
            try:
                create_table = gcs_to_bq.GoogleCloudStorageToBigQueryOperator(
                    task_id=f"create_temp_{table['table_name']}_table_in_bq",
                    bucket="dvdrental_project",
                    source_objects=f"{table['table_name']}/{table['table_name']}_{SEED_END_DATE_NODASH}.parquet",
                    destination_project_dataset_table=f"temp_table.{table['table_name']}_{SEED_END_DATE_NODASH}",
                    source_format="PARQUET",
                    create_disposition="CREATE_IF_NEEDED",
                    write_disposition="WRITE_TRUNCATE",
                    autodetect=True,
                    external_table=True,
                    gcp_conn_id=gcp_conn_id
                )
                create_table.execute(context=kwargs)
                logging.info(f"Successfully created external table in BQ: {table['table_name']}")
            except Exception as e:
                logging.error(e)
                raise e
        

        @task(
            task_id=f"delete_{table['table_name']}_from_local",
        )
        def delete_file(table, **kwargs):
            try:
                os.remove(f"{TMP_DATA_PATH}/{table['table_name']}_{SEED_END_DATE_NODASH}.parquet")
                logging.info(f"Successfully deleted file: {table['table_name']}_{SEED_END_DATE_NODASH}.parquet")
            except Exception as e:
                logging.error(e)
                raise e
            
        
        @task(
            task_id=f"dbt_upsert_{table['table_name']}_table",
        )
        def dbt_upsert(table, **kwargs):

            try:
                dbt_upsert = BashOperator(
                    task_id=f"dbt_upsert_{table['table_name']}_table",
                    bash_command=f"cd /dbt && dbt run --models raw_dvdrental.{table['table_name']} --vars '{{database: {temp_dataset}, table: {table['table_name']}_{SEED_END_DATE_NODASH}}}'",
                )
                dbt_upsert.execute(context=kwargs)
                logging.info(f"Successfully ran dbt incremental for table: {table['table_name']}")
            except Exception as e:
                logging.error(e)
                raise e
        

        extract_data = extract_postgres(table, postgres_conn_id)
        load_data = load_to_gcs(table, gcp_conn_id)
        create_table = create_temp_table_in_bq(table, gcp_conn_id)
        delete_file = delete_file(table)
        dbt_upsert = dbt_upsert(table)
    
        extract_data >> load_data >> [delete_file, create_table] >> dbt_upsert
        

el_pipeline()
