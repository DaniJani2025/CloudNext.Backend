import psycopg2
import bcrypt
import uuid
from datetime import datetime

# Database connection details
DB_NAME = "CloudNextDB"
DB_USER = "postgres"
DB_PASSWORD = "root"
DB_HOST = "localhost"
DB_PORT = "5432"

# Function to hash password
def hash_password(password: str) -> str:
    return bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt()).decode('utf-8')

# Function to add a user to the database
def add_user(email: str, password: str):
    user_id = str(uuid.uuid4())  # Generate UUID for user ID
    password_hash = hash_password(password)
    created_at = datetime.utcnow()
    
    try:
        # Connect to PostgreSQL database
        conn = psycopg2.connect(
            dbname=DB_NAME, user=DB_USER, password=DB_PASSWORD, host=DB_HOST, port=DB_PORT
        )
        cursor = conn.cursor()
        
        # Insert user into database
        query = '''
        INSERT INTO "Users" ("Id", "Email", "PasswordHash", "CreatedAt")
        VALUES (%s, %s, %s, %s);
        '''
        cursor.execute(query, (user_id, email, password_hash, created_at))
        
        # Commit and close connection
        conn.commit()
        cursor.close()
        conn.close()
        print("User added successfully!")
    except Exception as e:
        print("Error:", e)

# Example usage
if __name__ == "__main__":
    email_input = "dhananjayjani2000@gmail.com"
    password_input = "danijani"
    add_user(email_input, password_input)

# import bcrypt

# def verify_password(entered_password, stored_hashed_password):
#     if bcrypt.checkpw(entered_password.encode(), stored_hashed_password.encode()):
#         print("✅ Password is correct!")
#     else:
#         print("❌ Incorrect password.")

# # Manually enter details
# entered_password = "danijani"
# stored_hashed_password = "$2b$12$.U2t9Fmqo3PdyF.4zmAUs.iC51ZjvaNwImVDPt6a0RWlaD27/waga"

# verify_password(entered_password, stored_hashed_password)
