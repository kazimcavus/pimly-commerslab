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
-- Name: pricing; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA IF NOT EXISTS pricing;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: __ef_migrations_history; Type: TABLE; Schema: pricing; Owner: -
--

CREATE TABLE pricing.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: base_prices; Type: TABLE; Schema: pricing; Owner: -
--

CREATE TABLE pricing.base_prices (
    id uuid NOT NULL,
    product_item_id uuid NOT NULL,
    amount numeric(14,2) NOT NULL,
    compare_at_amount numeric(14,2),
    currency character varying(3) NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    tenant_id uuid NOT NULL
);


--
-- Name: channel_prices; Type: TABLE; Schema: pricing; Owner: -
--

CREATE TABLE pricing.channel_prices (
    id uuid NOT NULL,
    product_item_id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    amount numeric(14,2) NOT NULL,
    compare_at_amount numeric(14,2),
    currency character varying(3) NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    tenant_id uuid NOT NULL
);


--
-- Name: outbox_messages; Type: TABLE; Schema: pricing; Owner: -
--

CREATE TABLE pricing.outbox_messages (
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
-- Name: price_definitions; Type: TABLE; Schema: pricing; Owner: -
--

CREATE TABLE pricing.price_definitions (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    code character varying(100),
    tenant_id uuid NOT NULL
);


--
-- Name: product_item_prices; Type: TABLE; Schema: pricing; Owner: -
--

CREATE TABLE pricing.product_item_prices (
    id uuid NOT NULL,
    product_item_id uuid NOT NULL,
    price_definition_id uuid NOT NULL,
    amount numeric(14,2) NOT NULL,
    currency character varying(3) NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    tenant_id uuid NOT NULL
);


--
-- Name: __ef_migrations_history PK___ef_migrations_history; Type: CONSTRAINT; Schema: pricing; Owner: -
--

ALTER TABLE ONLY pricing.__ef_migrations_history
    ADD CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId");


--
-- Name: base_prices PK_base_prices; Type: CONSTRAINT; Schema: pricing; Owner: -
--

ALTER TABLE ONLY pricing.base_prices
    ADD CONSTRAINT "PK_base_prices" PRIMARY KEY (id);


--
-- Name: channel_prices PK_channel_prices; Type: CONSTRAINT; Schema: pricing; Owner: -
--

ALTER TABLE ONLY pricing.channel_prices
    ADD CONSTRAINT "PK_channel_prices" PRIMARY KEY (id);


--
-- Name: outbox_messages PK_outbox_messages; Type: CONSTRAINT; Schema: pricing; Owner: -
--

ALTER TABLE ONLY pricing.outbox_messages
    ADD CONSTRAINT "PK_outbox_messages" PRIMARY KEY (id);


--
-- Name: price_definitions PK_price_definitions; Type: CONSTRAINT; Schema: pricing; Owner: -
--

ALTER TABLE ONLY pricing.price_definitions
    ADD CONSTRAINT "PK_price_definitions" PRIMARY KEY (id);


--
-- Name: product_item_prices PK_product_item_prices; Type: CONSTRAINT; Schema: pricing; Owner: -
--

ALTER TABLE ONLY pricing.product_item_prices
    ADD CONSTRAINT "PK_product_item_prices" PRIMARY KEY (id);


--
-- Name: IX_base_prices_product_item_id; Type: INDEX; Schema: pricing; Owner: -
--

CREATE UNIQUE INDEX "IX_base_prices_product_item_id" ON pricing.base_prices USING btree (product_item_id);


--
-- Name: IX_channel_prices_product_item_id_marketplace_code; Type: INDEX; Schema: pricing; Owner: -
--

CREATE UNIQUE INDEX "IX_channel_prices_product_item_id_marketplace_code" ON pricing.channel_prices USING btree (product_item_id, marketplace_code);


--
-- Name: IX_outbox_messages_processed_on_utc_occurred_on_utc; Type: INDEX; Schema: pricing; Owner: -
--

CREATE INDEX "IX_outbox_messages_processed_on_utc_occurred_on_utc" ON pricing.outbox_messages USING btree (processed_on_utc, occurred_on_utc);


--
-- Name: IX_price_definitions_tenant_id_name; Type: INDEX; Schema: pricing; Owner: -
--

CREATE UNIQUE INDEX "IX_price_definitions_tenant_id_name" ON pricing.price_definitions USING btree (tenant_id, name);


--
-- Name: IX_product_item_prices_price_definition_id; Type: INDEX; Schema: pricing; Owner: -
--

CREATE INDEX "IX_product_item_prices_price_definition_id" ON pricing.product_item_prices USING btree (price_definition_id);


--
-- Name: IX_product_item_prices_product_item_id_price_definition_id; Type: INDEX; Schema: pricing; Owner: -
--

CREATE UNIQUE INDEX "IX_product_item_prices_product_item_id_price_definition_id" ON pricing.product_item_prices USING btree (product_item_id, price_definition_id);


--
-- Name: product_item_prices FK_product_item_prices_price_definitions_price_definition_id; Type: FK CONSTRAINT; Schema: pricing; Owner: -
--

ALTER TABLE ONLY pricing.product_item_prices
    ADD CONSTRAINT "FK_product_item_prices_price_definitions_price_definition_id" FOREIGN KEY (price_definition_id) REFERENCES pricing.price_definitions(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--


