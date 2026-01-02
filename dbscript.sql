--
-- PostgreSQL database dump
--

\restrict 3jfYZuNQnY3IisYZ3Gm7KjTn4zpScb9l7ZzMPcuojR1cXQY8bR6naBYj4fU5nRL

-- Dumped from database version 18.1
-- Dumped by pg_dump version 18.1

-- Started on 2025-12-15 17:14:53

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 5037 (class 1262 OID 16384)
-- Name: RAMA_TMS; Type: DATABASE; Schema: -; Owner: postgres
--

CREATE DATABASE "RAMA_TMS" WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE_PROVIDER = libc LOCALE = 'English_United States.1252';


ALTER DATABASE "RAMA_TMS" OWNER TO postgres;

\unrestrict 3jfYZuNQnY3IisYZ3Gm7KjTn4zpScb9l7ZzMPcuojR1cXQY8bR6naBYj4fU5nRL
\connect "RAMA_TMS"
\restrict 3jfYZuNQnY3IisYZ3Gm7KjTn4zpScb9l7ZzMPcuojR1cXQY8bR6naBYj4fU5nRL

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 4 (class 2615 OID 2200)
-- Name: public; Type: SCHEMA; Schema: -; Owner: pg_database_owner
--

CREATE SCHEMA public;


ALTER SCHEMA public OWNER TO pg_database_owner;

--
-- TOC entry 5038 (class 0 OID 0)
-- Dependencies: 4
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: pg_database_owner
--

COMMENT ON SCHEMA public IS 'standard public schema';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 220 (class 1259 OID 16408)
-- Name: DonorMaster; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."DonorMaster" (
    "DonorId" bigint NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "Phone" character varying(25),
    "Email" character varying(255),
    "Address1" character varying(255),
    "Address2" character varying(255),
    "City" character varying(100),
    "State" character varying(100),
    "Country" character varying(100),
    "PostalCode" character varying(20),
    "IsOrganization" boolean DEFAULT false NOT NULL,
    "OrganizationName" character varying(255),
    "TaxId" character varying(50),
    "DonorType" character varying(50),
    "PreferredContactMethod" character varying(20),
    "AllowEmail" boolean DEFAULT true NOT NULL,
    "AllowSms" boolean DEFAULT false NOT NULL,
    "AllowMail" boolean DEFAULT true NOT NULL,
    "Notes" text,
    "CreatedBy" character varying(100),
    "CreatedDate" timestamp with time zone DEFAULT now() NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdateDate" timestamp with time zone,
    "IsActive" boolean DEFAULT true NOT NULL
);


ALTER TABLE public."DonorMaster" OWNER TO postgres;

--
-- TOC entry 219 (class 1259 OID 16407)
-- Name: DonorMaster_DonorId_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."DonorMaster_DonorId_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DonorMaster_DonorId_seq" OWNER TO postgres;

--
-- TOC entry 5039 (class 0 OID 0)
-- Dependencies: 219
-- Name: DonorMaster_DonorId_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."DonorMaster_DonorId_seq" OWNED BY public."DonorMaster"."DonorId";


--
-- TOC entry 223 (class 1259 OID 16489)
-- Name: DonorMaster_DonorId_seq1; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."DonorMaster" ALTER COLUMN "DonorId" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."DonorMaster_DonorId_seq1"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 222 (class 1259 OID 16430)
-- Name: DonorReceiptDetail; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."DonorReceiptDetail" (
    "DonorReceiptDetailId" bigint NOT NULL,
    "DonorId" bigint NOT NULL,
    "DonationAmt" numeric(12,2) NOT NULL,
    "DonationType" character varying(100) NOT NULL,
    "Currency" character varying(10) DEFAULT 'USD'::character varying NOT NULL,
    "DateOfDonation" timestamp with time zone DEFAULT now() NOT NULL,
    "PaymentMethod" character varying(20),
    "PaymentReference" character varying(100),
    "IsTaxDeductible" boolean DEFAULT true NOT NULL,
    "IsAnonymous" boolean DEFAULT false NOT NULL,
    "InternalNotes" text,
    "CreatedBy" character varying(100),
    "CreatedDate" timestamp with time zone DEFAULT now() NOT NULL,
    "UpdatedBy" character varying(100),
    "UpdateDate" timestamp with time zone,
    "IsActive" boolean DEFAULT true NOT NULL
);


ALTER TABLE public."DonorReceiptDetail" OWNER TO postgres;

--
-- TOC entry 221 (class 1259 OID 16429)
-- Name: DonorReceiptDetail_DonorReceiptDetailId_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."DonorReceiptDetail_DonorReceiptDetailId_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."DonorReceiptDetail_DonorReceiptDetailId_seq" OWNER TO postgres;

--
-- TOC entry 5040 (class 0 OID 0)
-- Dependencies: 221
-- Name: DonorReceiptDetail_DonorReceiptDetailId_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."DonorReceiptDetail_DonorReceiptDetailId_seq" OWNED BY public."DonorReceiptDetail"."DonorReceiptDetailId";


--
-- TOC entry 4868 (class 2604 OID 16433)
-- Name: DonorReceiptDetail DonorReceiptDetailId; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DonorReceiptDetail" ALTER COLUMN "DonorReceiptDetailId" SET DEFAULT nextval('public."DonorReceiptDetail_DonorReceiptDetailId_seq"'::regclass);


--
-- TOC entry 5028 (class 0 OID 16408)
-- Dependencies: 220
-- Data for Name: DonorMaster; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."DonorMaster" OVERRIDING SYSTEM VALUE VALUES (1, 'Raghavendra', 'Parimala', '999999999', 'mantralaya_Raghavendra@srsmutt.org', 'SRS Mutt', 'Tunga Teera', 'Kurnool', 'Andhra Pradesh', 'India', '5742256', false, NULL, NULL, 'Individual', NULL, true, false, true, 'om sri guru raghavendraya namaha!', 'system', '2025-11-30 20:41:23.7863-05', NULL, NULL, true);
INSERT INTO public."DonorMaster" OVERRIDING SYSTEM VALUE VALUES (2, 'Rekha', 'Pradeep', '4041546958', 'rekha_rao@gmail.com', '1060 Rocky Road', NULL, 'Lawrenceville', 'GA', 'United States', '30066', false, NULL, NULL, 'Individual', NULL, true, false, true, 'Rayara Matha Foundation Donation', 'system', '2025-11-30 20:42:45.944352-05', NULL, NULL, true);
INSERT INTO public."DonorMaster" OVERRIDING SYSTEM VALUE VALUES (3, 'Tejasvi', 'Mahesh', '5139089079', 'tejasvimys@gmail.com', '627 E Lake Dr', NULL, 'Marietta', 'GA', 'United States', '30062', false, NULL, NULL, 'Individual', NULL, true, false, true, 'Audio System Donation', 'system', '2025-11-30 20:43:34.12716-05', NULL, NULL, true);
INSERT INTO public."DonorMaster" OVERRIDING SYSTEM VALUE VALUES (4, 'Pradeep', 'Vitthalamurthy', '4042561458', 'pradeep_vittallamurthy@gmail.com', '1060 Rocky Road', NULL, 'Lawrenceville', 'GA', 'United States', '30066', false, NULL, NULL, 'Individual', NULL, true, false, true, 'Ananthaadi Rayara Matha (RAMA)', 'system', '2025-11-30 20:47:19.537228-05', NULL, NULL, true);
INSERT INTO public."DonorMaster" OVERRIDING SYSTEM VALUE VALUES (10, 'Pradeep', 'Vittalmurthy', NULL, 'pradeep16@hotmail.com', NULL, NULL, NULL, NULL, NULL, NULL, false, NULL, NULL, 'Individual', NULL, true, false, true, 'Created via bulk upload', 'bulk-import', '2025-12-02 20:54:55.002663-05', NULL, NULL, true);
INSERT INTO public."DonorMaster" OVERRIDING SYSTEM VALUE VALUES (11, 'Rekha', 'Rao', NULL, 'rekharao@gmail.com', NULL, NULL, NULL, NULL, NULL, NULL, false, NULL, NULL, 'Individual', NULL, true, false, true, 'Created via bulk upload', 'bulk-import', '2025-12-02 20:54:55.002693-05', NULL, NULL, true);
INSERT INTO public."DonorMaster" OVERRIDING SYSTEM VALUE VALUES (12, 'Tejasvi', 'Mahesh', NULL, 'tejasvimahesh@gmail.com', NULL, NULL, NULL, NULL, NULL, NULL, false, NULL, NULL, 'Individual', NULL, true, false, true, 'Created via bulk upload', 'bulk-import', '2025-12-02 20:54:55.002709-05', NULL, NULL, true);
INSERT INTO public."DonorMaster" OVERRIDING SYSTEM VALUE VALUES (13, 'Supritha', 'Tejasvi', NULL, 'suprithatejasvi@gmail.com', NULL, NULL, NULL, NULL, NULL, NULL, false, NULL, NULL, 'Individual', NULL, true, false, true, 'Created via bulk upload', 'bulk-import', '2025-12-02 20:54:55.002858-05', NULL, NULL, true);
INSERT INTO public."DonorMaster" OVERRIDING SYSTEM VALUE VALUES (14, 'Pradeep', 'Vittalmurthy', NULL, 'pradeepvittalmurthy@gmail.com', NULL, NULL, NULL, NULL, NULL, NULL, false, NULL, NULL, 'Individual', NULL, true, false, true, 'Created via bulk upload', 'bulk-import', '2025-12-02 20:54:55.002971-05', NULL, NULL, true);


--
-- TOC entry 5030 (class 0 OID 16430)
-- Dependencies: 222
-- Data for Name: DonorReceiptDetail; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."DonorReceiptDetail" VALUES (1, 3, 100.00, 'General', 'USD', '2025-11-30 22:50:00.215-05', 'Cash', 'Cash', false, false, 'Cash donation for ', 'system', '2025-11-30 22:50:00.349983-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (2, 4, 250.00, 'BuildingFund', 'USD', '2025-11-29 19:00:00-05', 'Check', 'VR2541201', false, false, 'Check received.', 'system', '2025-11-30 22:54:28.520954-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (3, 10, 62.00, 'General', 'USD', '2025-05-27 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002674-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (4, 11, 48.00, 'General', 'USD', '2025-03-11 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002696-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (5, 12, 61.00, 'General', 'USD', '2025-09-27 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002711-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (6, 10, 49.00, 'General', 'USD', '2025-11-28 00:00:00-05', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002738-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (7, 10, 36.00, 'General', 'USD', '2025-12-31 00:00:00-05', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002754-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (8, 10, 84.00, 'General', 'USD', '2025-11-24 00:00:00-05', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002766-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (9, 10, 41.00, 'General', 'USD', '2025-12-15 00:00:00-05', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002775-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (10, 10, 97.00, 'General', 'USD', '2025-08-21 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.0028-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (11, 10, 33.00, 'General', 'USD', '2025-11-25 00:00:00-05', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002815-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (12, 11, 3.00, 'General', 'USD', '2025-03-25 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002833-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (13, 12, 33.00, 'General', 'USD', '2025-07-19 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002847-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (14, 13, 28.00, 'General', 'USD', '2025-03-25 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002862-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (15, 11, 65.00, 'General', 'USD', '2025-10-24 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002874-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (16, 12, 50.00, 'General', 'USD', '2025-01-24 00:00:00-05', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002901-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (17, 13, 53.00, 'General', 'USD', '2025-06-13 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002915-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (18, 12, 69.00, 'General', 'USD', '2025-08-18 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002922-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (19, 13, 99.00, 'General', 'USD', '2025-04-02 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002928-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (20, 11, 17.00, 'General', 'USD', '2025-08-17 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002935-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (21, 14, 77.00, 'General', 'USD', '2025-10-14 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002974-05', NULL, NULL, true);
INSERT INTO public."DonorReceiptDetail" VALUES (22, 14, 66.00, 'General', 'USD', '2025-07-03 00:00:00-04', 'Cash', NULL, true, false, 'Bulk upload 2025', 'bulk-import', '2025-12-02 20:54:55.002995-05', NULL, NULL, true);


--
-- TOC entry 5041 (class 0 OID 0)
-- Dependencies: 219
-- Name: DonorMaster_DonorId_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."DonorMaster_DonorId_seq"', 1, true);


--
-- TOC entry 5042 (class 0 OID 0)
-- Dependencies: 223
-- Name: DonorMaster_DonorId_seq1; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."DonorMaster_DonorId_seq1"', 14, true);


--
-- TOC entry 5043 (class 0 OID 0)
-- Dependencies: 221
-- Name: DonorReceiptDetail_DonorReceiptDetailId_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."DonorReceiptDetail_DonorReceiptDetailId_seq"', 22, true);


--
-- TOC entry 4876 (class 2606 OID 16482)
-- Name: DonorMaster DonorMaster_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DonorMaster"
    ADD CONSTRAINT "DonorMaster_pkey" PRIMARY KEY ("DonorId");


--
-- TOC entry 4878 (class 2606 OID 16451)
-- Name: DonorReceiptDetail DonorReceiptDetail_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DonorReceiptDetail"
    ADD CONSTRAINT "DonorReceiptDetail_pkey" PRIMARY KEY ("DonorReceiptDetailId");


--
-- TOC entry 4879 (class 2606 OID 16484)
-- Name: DonorReceiptDetail FK_DonorReceiptDetail_DonorMaster_DonorId; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DonorReceiptDetail"
    ADD CONSTRAINT "FK_DonorReceiptDetail_DonorMaster_DonorId" FOREIGN KEY ("DonorId") REFERENCES public."DonorMaster"("DonorId") ON DELETE RESTRICT;


-- Completed on 2025-12-15 17:14:53

--
-- PostgreSQL database dump complete
--

\unrestrict 3jfYZuNQnY3IisYZ3Gm7KjTn4zpScb9l7ZzMPcuojR1cXQY8bR6naBYj4fU5nRL

-- Add 2FA columns to AppUsers table
ALTER TABLE public."AppUsers" 
ADD COLUMN "TwoFactorEnabled" boolean NOT NULL DEFAULT false,
ADD COLUMN "TwoFactorSecret" character varying(255),
ADD COLUMN "BackupCodes" text[];

-- Optional: Add index for faster lookups
CREATE INDEX IF NOT EXISTS "IX_AppUsers_Email" ON public."AppUsers" ("Email");

COMMENT ON TABLE "AppUsers" IS 'Application users with authentication and authorization';
COMMENT ON COLUMN "AppUsers"."Role" IS 'User roles: Admin, Collector, Viewer';
COMMENT ON COLUMN "AppUsers"."IsActive" IS 'Account status - must be true to login';

-- Add authentication-related columns to AppUsers table (PostgreSQL)

-- Add PasswordHash column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'PasswordHash') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "PasswordHash" VARCHAR(500);
    END IF;
END $$;

-- Add RefreshToken column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'RefreshToken') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "RefreshToken" VARCHAR(500);
    END IF;
END $$;

-- Add RefreshTokenExpiryTime column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'RefreshTokenExpiryTime') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "RefreshTokenExpiryTime" TIMESTAMP;
    END IF;
END $$;

-- Add LastLoginDate column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'LastLoginDate') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "LastLoginDate" TIMESTAMP;
    END IF;
END $$;

-- Add IsActive column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'IsActive') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "IsActive" BOOLEAN NOT NULL DEFAULT TRUE;
    END IF;
END $$;

-- Add Email column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'Email') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "Email" VARCHAR(255);
    END IF;
END $$;

-- Add FirstName column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'FirstName') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "FirstName" VARCHAR(100);
    END IF;
END $$;

-- Add LastName column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'LastName') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "LastName" VARCHAR(100);
    END IF;
END $$;

-- Add PhoneNumber column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'PhoneNumber') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "PhoneNumber" VARCHAR(20);
    END IF;
END $$;

-- Add RoleId column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'RoleId') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "RoleId" INTEGER;
    END IF;
END $$;

-- Add CreatedDate column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'CreatedDate') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "CreatedDate" TIMESTAMP NOT NULL DEFAULT NOW();
    END IF;
END $$;

-- Add CreatedBy column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'CreatedBy') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "CreatedBy" INTEGER;
    END IF;
END $$;

-- Add ModifiedDate column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'ModifiedDate') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "ModifiedDate" TIMESTAMP;
    END IF;
END $$;

-- Add ModifiedBy column if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'AppUsers' AND column_name = 'ModifiedBy') THEN
        ALTER TABLE "AppUsers" ADD COLUMN "ModifiedBy" INTEGER;
    END IF;
END $$;

-- Create unique index on Email if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes 
                   WHERE indexname = 'idx_appusers_email') THEN
        CREATE UNIQUE INDEX idx_appusers_email ON "AppUsers"("Email") WHERE "Email" IS NOT NULL;
    END IF;
END $$;

-- Create index on RefreshToken for faster lookups
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_indexes 
                   WHERE indexname = 'idx_appusers_refreshtoken') THEN
        CREATE INDEX idx_appusers_refreshtoken ON "AppUsers"("RefreshToken") WHERE "RefreshToken" IS NOT NULL;
    END IF;
END $$;

-- Print success message
DO $$ 
BEGIN
    RAISE NOTICE 'AppUsers table altered successfully with authentication columns.';
END $$;

--adding 2FA related scripts
-- Add 2FA columns to AppUsers table (adjust table name if different)
ALTER TABLE "AppUsers" 
ADD COLUMN IF NOT EXISTS "TwoFactorEnabled" boolean NOT NULL DEFAULT false,
ADD COLUMN IF NOT EXISTS "TwoFactorSecret" character varying(255),
ADD COLUMN IF NOT EXISTS "BackupCodes" text[];

-- Optional: Add comment to columns for documentation
COMMENT ON COLUMN "AppUsers"."TwoFactorEnabled" IS 'Indicates if 2FA is enabled for this user';
COMMENT ON COLUMN "AppUsers"."TwoFactorSecret" IS 'TOTP secret key for 2FA';
COMMENT ON COLUMN "AppUsers"."BackupCodes" IS 'Array of backup recovery codes';

-- Verify the columns were added
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'AppUsers' 
AND column_name IN ('TwoFactorEnabled', 'TwoFactorSecret', 'BackupCodes');

-- Disable 2FA for all users (or specific user by email)
UPDATE "AppUsers" 
SET "TwoFactorEnabled" = false,
    "TwoFactorSecret" = NULL,
    "BackupCodes" = NULL;

-- Or disable for specific user only (replace with your email)
UPDATE "AppUsers" 
SET "TwoFactorEnabled" = false,
    "TwoFactorSecret" = NULL,
    "BackupCodes" = NULL
WHERE "Email" = 'tejasvimys@gmail.com';

