-- Enable PostgreSQL extensions for advanced features
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "unaccent";

-- Create text search configuration for Nepali content
CREATE TEXT SEARCH CONFIGURATION IF NOT EXISTS nepali (COPY = simple);