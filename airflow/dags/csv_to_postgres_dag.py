from datetime import datetime, timedelta
from airflow import DAG
from airflow.operators.python import PythonOperator
from airflow.providers.postgres.hooks.postgres import PostgresHook
import pandas as pd
import logging
from typing import List, Dict

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

default_args = {
    'owner': 'airflow',
    'depends_on_past': False,
    'start_date': datetime(2024, 2, 7),
    'email_on_failure': False,
    'email_on_retry': False,
    'retries': 1,
    'retry_delay': timedelta(minutes=5)
}

def read_and_validate_csv(**context):
    """
    Read CSV file and perform initial validation
    """
    try:
        df = pd.read_csv('/opt/airflow/data/hdb-carpark-information-20220824010400.csv')
        required_columns = [
            "car_park_no", "address", "x_coord", "y_coord", 
            "car_park_type", "type_of_parking_system", "short_term_parking",
            "free_parking", "night_parking", "car_park_decks",
            "gantry_height", "car_park_basement"
        ]
        
        # Validate columns
        missing_cols = set(required_columns) - set(df.columns)
        if missing_cols:
            raise ValueError(f"Missing required columns: {missing_cols}")
            
        # Basic data cleaning
        df = df.fillna({
            'car_park_decks': 1,
            'gantry_height': 0.0,
            'car_park_basement': 'N'
        })
        
        # Convert boolean fields
        df['free_parking'] = df['free_parking'].map({'Y': True, 'N': False})
        df['night_parking'] = df['night_parking'].map({'Y': True, 'N': False})
        df['car_park_basement'] = df['car_park_basement'].map({'Y': True, 'N': False})
        
        # Push to XCom
        context['task_instance'].xcom_push(key='raw_data', value=df.to_dict(orient='records'))
        logger.info(f"Successfully processed {len(df)} records")
        
    except Exception as e:
        logger.error(f"Error in read_and_validate_csv: {str(e)}")
        raise

def setup_lookup_tables(**context):
    """
    Create and populate lookup tables if they don't exist
    """
    try:
        pg_hook = PostgresHook(postgres_conn_id='postgres_default')
        
        # Create lookup tables
        create_tables_sql = """
        CREATE TABLE IF NOT EXISTS car_park_type (
            id SERIAL PRIMARY KEY,
            type_name VARCHAR(100) UNIQUE
        );
        
        CREATE TABLE IF NOT EXISTS parking_system (
            id SERIAL PRIMARY KEY,
            system_name VARCHAR(100) UNIQUE
        );
        
        CREATE TABLE IF NOT EXISTS short_term_parking (
            id SERIAL PRIMARY KEY,
            description VARCHAR(100) UNIQUE
        );
        
        CREATE TABLE IF NOT EXISTS car_park (
            id SERIAL PRIMARY KEY,
            car_park_no VARCHAR(20) UNIQUE,
            address TEXT,
            x_coord DOUBLE PRECISION,
            y_coord DOUBLE PRECISION,
            car_park_type_id INTEGER REFERENCES car_park_type(id),
            parking_system_id INTEGER REFERENCES parking_system(id),
            short_term_parking_id INTEGER REFERENCES short_term_parking(id),
            free_parking BOOLEAN,
            night_parking BOOLEAN,
            car_park_decks INTEGER,
            gantry_height REAL,
            car_park_basement BOOLEAN
        );
        """
        pg_hook.run(create_tables_sql)
        
        # Get raw data
        raw_data = context['task_instance'].xcom_pull(key='raw_data', task_ids='read_and_validate_csv')
        df = pd.DataFrame(raw_data)
        
        # Extract unique values for lookup tables
        car_park_types = pd.unique(df['car_park_type']).tolist()
        parking_systems = pd.unique(df['type_of_parking_system']).tolist()
        short_term_parkings = pd.unique(df['short_term_parking']).tolist()
        
        # Insert into lookup tables
        with pg_hook.get_conn() as conn:
            with conn.cursor() as cur:
                # Car park types
                for type_name in car_park_types:
                    cur.execute(
                        "INSERT INTO car_park_type (type_name) VALUES (%s) ON CONFLICT (type_name) DO NOTHING",
                        (type_name,)
                    )
                
                # Parking systems
                for system_name in parking_systems:
                    cur.execute(
                        "INSERT INTO parking_system (system_name) VALUES (%s) ON CONFLICT (system_name) DO NOTHING",
                        (system_name,)
                    )
                
                # Short term parking
                for description in short_term_parkings:
                    cur.execute(
                        "INSERT INTO short_term_parking (description) VALUES (%s) ON CONFLICT (description) DO NOTHING",
                        (description,)
                    )
                
            conn.commit()
        
        logger.info("Successfully set up lookup tables")
        
    except Exception as e:
        logger.error(f"Error in setup_lookup_tables: {str(e)}")
        raise

def transform_and_load_main_table(**context):
    """
    Transform data and load into main car_park table
    """
    try:
        pg_hook = PostgresHook(postgres_conn_id='postgres_default')
        raw_data = context['task_instance'].xcom_pull(key='raw_data', task_ids='read_and_validate_csv')
        df = pd.DataFrame(raw_data)
        
        # Get lookup table mappings
        with pg_hook.get_conn() as conn:
            with conn.cursor() as cur:
                # Get car park types mapping
                cur.execute("SELECT id, type_name FROM car_park_type")
                car_park_type_map = dict(cur.fetchall())
                car_park_type_map = {v: k for k, v in car_park_type_map.items()}
                
                # Get parking systems mapping
                cur.execute("SELECT id, system_name FROM parking_system")
                parking_system_map = dict(cur.fetchall())
                parking_system_map = {v: k for k, v in parking_system_map.items()}
                
                # Get short term parking mapping
                cur.execute("SELECT id, description FROM short_term_parking")
                short_term_parking_map = dict(cur.fetchall())
                short_term_parking_map = {v: k for k, v in short_term_parking_map.items()}
        
        # Transform data
        transformed_data = []
        for record in raw_data:
            transformed_record = {
                'car_park_no': record['car_park_no'],
                'address': record['address'],
                'x_coord': record['x_coord'],
                'y_coord': record['y_coord'],
                'car_park_type_id': car_park_type_map[record['car_park_type']],
                'parking_system_id': parking_system_map[record['type_of_parking_system']],
                'short_term_parking_id': short_term_parking_map[record['short_term_parking']],
                'free_parking': record['free_parking'],
                'night_parking': record['night_parking'],
                'car_park_decks': record['car_park_decks'],
                'gantry_height': record['gantry_height'],
                'car_park_basement': record['car_park_basement']
            }
            transformed_data.append(transformed_record)
        
        # Insert into main table
        with pg_hook.get_conn() as conn:
            with conn.cursor() as cur:
                for record in transformed_data:
                    cur.execute("""
                        INSERT INTO car_park (
                            car_park_no, address, x_coord, y_coord,
                            car_park_type_id, parking_system_id, short_term_parking_id,
                            free_parking, night_parking, car_park_decks,
                            gantry_height, car_park_basement
                        ) VALUES (
                            %(car_park_no)s, %(address)s, %(x_coord)s, %(y_coord)s,
                            %(car_park_type_id)s, %(parking_system_id)s, %(short_term_parking_id)s,
                            %(free_parking)s, %(night_parking)s, %(car_park_decks)s,
                            %(gantry_height)s, %(car_park_basement)s
                        )
                        ON CONFLICT (car_park_no) DO UPDATE SET
                            address = EXCLUDED.address,
                            x_coord = EXCLUDED.x_coord,
                            y_coord = EXCLUDED.y_coord,
                            car_park_type_id = EXCLUDED.car_park_type_id,
                            parking_system_id = EXCLUDED.parking_system_id,
                            short_term_parking_id = EXCLUDED.short_term_parking_id,
                            free_parking = EXCLUDED.free_parking,
                            night_parking = EXCLUDED.night_parking,
                            car_park_decks = EXCLUDED.car_park_decks,
                            gantry_height = EXCLUDED.gantry_height,
                            car_park_basement = EXCLUDED.car_park_basement
                    """, record)
            conn.commit()
        
        logger.info(f"Successfully loaded {len(transformed_data)} records into car_park table")
        
    except Exception as e:
        logger.error(f"Error in transform_and_load_main_table: {str(e)}")
        raise

# Create DAG
dag = DAG(
    'csv_to_postgres_dag',
    default_args=default_args,
    description='ETL pipeline for normalized car park data',
    schedule_interval=timedelta(days=1),
    catchup=False
)

# Define tasks
read_csv_task = PythonOperator(
    task_id='read_and_validate_csv',
    python_callable=read_and_validate_csv,
    provide_context=True,
    dag=dag
)

setup_lookup_tables_task = PythonOperator(
    task_id='setup_lookup_tables',
    python_callable=setup_lookup_tables,
    provide_context=True,
    dag=dag
)

load_main_table_task = PythonOperator(
    task_id='transform_and_load_main_table',
    python_callable=transform_and_load_main_table,
    provide_context=True,
    dag=dag
)

# Set dependencies
read_csv_task >> setup_lookup_tables_task >> load_main_table_task