-- Uygulama DB zaten POSTGRES_DB env ile oluşturulur.
-- Bu script ek DB'ler veya extension'lar için kullanılır.

-- UUID desteği (Npgsql uuid-ossp kullanmaz ama gen_random_uuid() için pgcrypto)
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Test DB (CI/CD pipeline için)
CREATE DATABASE vibexlearn_test
    WITH OWNER = vibex_user
    ENCODING = 'UTF8'
    TEMPLATE = template0;

GRANT ALL PRIVILEGES ON DATABASE vibexlearn_test TO vibex_user;
