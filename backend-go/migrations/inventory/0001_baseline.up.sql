--
-- PostgreSQL database dump
--


-- Dumped from database version 17.10
-- Dumped by pg_dump version 17.10

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
-- Name: inventory; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA IF NOT EXISTS inventory;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: __ef_migrations_history; Type: TABLE; Schema: inventory; Owner: -
--

CREATE TABLE inventory.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: outbox_messages; Type: TABLE; Schema: inventory; Owner: -
--

CREATE TABLE inventory.outbox_messages (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    type character varying(500) NOT NULL,
    payload jsonb NOT NULL,
    occurred_on_utc timestamp with time zone NOT NULL,
    processed_on_utc timestamp with time zone,
    attempts integer NOT NULL,
    error text
);


--
-- Name: stock_levels; Type: TABLE; Schema: inventory; Owner: -
--

CREATE TABLE inventory.stock_levels (
    id uuid NOT NULL,
    product_item_id uuid NOT NULL,
    quantity integer NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    tenant_id uuid NOT NULL
);


--
-- Name: __ef_migrations_history PK___ef_migrations_history; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.__ef_migrations_history
    ADD CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId");


--
-- Name: outbox_messages PK_outbox_messages; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.outbox_messages
    ADD CONSTRAINT "PK_outbox_messages" PRIMARY KEY (id);


--
-- Name: stock_levels PK_stock_levels; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.stock_levels
    ADD CONSTRAINT "PK_stock_levels" PRIMARY KEY (id);


--
-- Name: IX_outbox_messages_processed_on_utc_occurred_on_utc; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX "IX_outbox_messages_processed_on_utc_occurred_on_utc" ON inventory.outbox_messages USING btree (processed_on_utc, occurred_on_utc);


--
-- Name: IX_stock_levels_product_item_id; Type: INDEX; Schema: inventory; Owner: -
--

CREATE UNIQUE INDEX "IX_stock_levels_product_item_id" ON inventory.stock_levels USING btree (product_item_id);


--
-- PostgreSQL database dump complete
--


