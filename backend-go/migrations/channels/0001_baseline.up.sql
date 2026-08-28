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
-- Name: channels; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA IF NOT EXISTS channels;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: __ef_migrations_history; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: attribute_channel_mappings; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.attribute_channel_mappings (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    catalog_category_id uuid NOT NULL,
    source_type character varying(30) NOT NULL,
    catalog_source_id uuid NOT NULL,
    external_attribute_id character varying(100) NOT NULL
);


--
-- Name: attribute_value_channel_mappings; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.attribute_value_channel_mappings (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    attribute_channel_mapping_id uuid NOT NULL,
    catalog_value_id uuid NOT NULL,
    external_value_id character varying(100) NOT NULL
);


--
-- Name: category_channel_mappings; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.category_channel_mappings (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    catalog_category_id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    external_id character varying(100) NOT NULL
);


--
-- Name: external_attribute_values; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.external_attribute_values (
    id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    external_category_id character varying(100) NOT NULL,
    external_attribute_id character varying(100) NOT NULL,
    external_value_id character varying(100) NOT NULL,
    name character varying(500) NOT NULL,
    synced_at timestamp with time zone NOT NULL
);


--
-- Name: external_categories; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.external_categories (
    id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    external_id character varying(100) NOT NULL,
    name character varying(500) NOT NULL,
    parent_external_id character varying(100),
    path character varying(2000) NOT NULL,
    is_leaf boolean NOT NULL,
    synced_at timestamp with time zone NOT NULL
);


--
-- Name: external_category_attributes; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.external_category_attributes (
    id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    external_category_id character varying(100) NOT NULL,
    external_attribute_id character varying(100) NOT NULL,
    name character varying(500) NOT NULL,
    required boolean NOT NULL,
    allow_custom boolean NOT NULL,
    is_variant boolean NOT NULL,
    is_slicer boolean NOT NULL,
    synced_at timestamp with time zone NOT NULL
);


--
-- Name: marketplace_connections; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.marketplace_connections (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    seller_id character varying(200),
    api_key character varying(500) NOT NULL,
    api_secret character varying(500),
    is_enabled boolean NOT NULL
);


--
-- Name: product_import_run_errors; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.product_import_run_errors (
    id uuid NOT NULL,
    product_main_id character varying(200) NOT NULL,
    barcode character varying(200),
    message character varying(1000) NOT NULL,
    product_import_run_id uuid NOT NULL
);


--
-- Name: product_import_runs; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.product_import_runs (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    status character varying(30) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    total_products integer,
    processed_products integer NOT NULL,
    imported_products integer NOT NULL,
    skipped_products integer NOT NULL,
    failed_products integer NOT NULL,
    error_message character varying(2000)
);


--
-- Name: product_listings; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.product_listings (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    product_item_id uuid NOT NULL,
    status character varying(30) NOT NULL,
    external_listing_id character varying(200),
    submission_reference character varying(200),
    content_hash character varying(64),
    offer_hash character varying(64),
    content_dirty_at timestamp with time zone,
    offer_dirty_at timestamp with time zone,
    last_submitted_at timestamp with time zone,
    last_confirmed_at timestamp with time zone,
    rejection_reason character varying(1000),
    sync_attempts integer NOT NULL,
    next_attempt_at timestamp with time zone
);


--
-- Name: product_publication_run_errors; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.product_publication_run_errors (
    id uuid NOT NULL,
    product_item_id uuid NOT NULL,
    message character varying(1000) NOT NULL,
    product_publication_run_id uuid NOT NULL
);


--
-- Name: product_publication_runs; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.product_publication_runs (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    status character varying(30) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    total_items integer,
    processed_items integer NOT NULL,
    published_items integer NOT NULL,
    failed_items integer NOT NULL,
    error_message character varying(2000)
);


--
-- Name: taxonomy_sync_runs; Type: TABLE; Schema: channels; Owner: -
--

CREATE TABLE channels.taxonomy_sync_runs (
    id uuid NOT NULL,
    marketplace_code character varying(10) NOT NULL,
    status character varying(20) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    started_at timestamp with time zone,
    completed_at timestamp with time zone,
    processed_count integer NOT NULL,
    total_estimate integer,
    error_message character varying(2000)
);


--
-- Name: __ef_migrations_history PK___ef_migrations_history; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.__ef_migrations_history
    ADD CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId");


--
-- Name: attribute_channel_mappings PK_attribute_channel_mappings; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.attribute_channel_mappings
    ADD CONSTRAINT "PK_attribute_channel_mappings" PRIMARY KEY (id);


--
-- Name: attribute_value_channel_mappings PK_attribute_value_channel_mappings; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.attribute_value_channel_mappings
    ADD CONSTRAINT "PK_attribute_value_channel_mappings" PRIMARY KEY (id);


--
-- Name: category_channel_mappings PK_category_channel_mappings; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.category_channel_mappings
    ADD CONSTRAINT "PK_category_channel_mappings" PRIMARY KEY (id);


--
-- Name: external_attribute_values PK_external_attribute_values; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.external_attribute_values
    ADD CONSTRAINT "PK_external_attribute_values" PRIMARY KEY (id);


--
-- Name: external_categories PK_external_categories; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.external_categories
    ADD CONSTRAINT "PK_external_categories" PRIMARY KEY (id);


--
-- Name: external_category_attributes PK_external_category_attributes; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.external_category_attributes
    ADD CONSTRAINT "PK_external_category_attributes" PRIMARY KEY (id);


--
-- Name: marketplace_connections PK_marketplace_connections; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.marketplace_connections
    ADD CONSTRAINT "PK_marketplace_connections" PRIMARY KEY (id);


--
-- Name: product_import_run_errors PK_product_import_run_errors; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.product_import_run_errors
    ADD CONSTRAINT "PK_product_import_run_errors" PRIMARY KEY (id);


--
-- Name: product_import_runs PK_product_import_runs; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.product_import_runs
    ADD CONSTRAINT "PK_product_import_runs" PRIMARY KEY (id);


--
-- Name: product_listings PK_product_listings; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.product_listings
    ADD CONSTRAINT "PK_product_listings" PRIMARY KEY (id);


--
-- Name: product_publication_run_errors PK_product_publication_run_errors; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.product_publication_run_errors
    ADD CONSTRAINT "PK_product_publication_run_errors" PRIMARY KEY (id);


--
-- Name: product_publication_runs PK_product_publication_runs; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.product_publication_runs
    ADD CONSTRAINT "PK_product_publication_runs" PRIMARY KEY (id);


--
-- Name: taxonomy_sync_runs PK_taxonomy_sync_runs; Type: CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.taxonomy_sync_runs
    ADD CONSTRAINT "PK_taxonomy_sync_runs" PRIMARY KEY (id);


--
-- Name: IX_attribute_channel_mappings_tenant_id_marketplace_code_catal~; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_attribute_channel_mappings_tenant_id_marketplace_code_catal~" ON channels.attribute_channel_mappings USING btree (tenant_id, marketplace_code, catalog_category_id);


--
-- Name: IX_attribute_channel_mappings_tenant_id_marketplace_code_cata~1; Type: INDEX; Schema: channels; Owner: -
--

CREATE UNIQUE INDEX "IX_attribute_channel_mappings_tenant_id_marketplace_code_cata~1" ON channels.attribute_channel_mappings USING btree (tenant_id, marketplace_code, catalog_category_id, source_type, catalog_source_id);


--
-- Name: IX_attribute_value_channel_mappings_attribute_channel_mapping_~; Type: INDEX; Schema: channels; Owner: -
--

CREATE UNIQUE INDEX "IX_attribute_value_channel_mappings_attribute_channel_mapping_~" ON channels.attribute_value_channel_mappings USING btree (attribute_channel_mapping_id, catalog_value_id);


--
-- Name: IX_category_channel_mappings_tenant_id_catalog_category_id_mar~; Type: INDEX; Schema: channels; Owner: -
--

CREATE UNIQUE INDEX "IX_category_channel_mappings_tenant_id_catalog_category_id_mar~" ON channels.category_channel_mappings USING btree (tenant_id, catalog_category_id, marketplace_code);


--
-- Name: IX_category_channel_mappings_tenant_id_marketplace_code; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_category_channel_mappings_tenant_id_marketplace_code" ON channels.category_channel_mappings USING btree (tenant_id, marketplace_code);


--
-- Name: IX_category_channel_mappings_tenant_id_marketplace_code_extern~; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_category_channel_mappings_tenant_id_marketplace_code_extern~" ON channels.category_channel_mappings USING btree (tenant_id, marketplace_code, external_id);


--
-- Name: IX_external_attribute_values_marketplace_code_external_categor~; Type: INDEX; Schema: channels; Owner: -
--

CREATE UNIQUE INDEX "IX_external_attribute_values_marketplace_code_external_categor~" ON channels.external_attribute_values USING btree (marketplace_code, external_category_id, external_attribute_id, external_value_id);


--
-- Name: IX_external_categories_marketplace_code; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_external_categories_marketplace_code" ON channels.external_categories USING btree (marketplace_code);


--
-- Name: IX_external_categories_marketplace_code_external_id; Type: INDEX; Schema: channels; Owner: -
--

CREATE UNIQUE INDEX "IX_external_categories_marketplace_code_external_id" ON channels.external_categories USING btree (marketplace_code, external_id);


--
-- Name: IX_external_categories_name; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_external_categories_name" ON channels.external_categories USING btree (name);


--
-- Name: IX_external_category_attributes_marketplace_code_external_cate~; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_external_category_attributes_marketplace_code_external_cate~" ON channels.external_category_attributes USING btree (marketplace_code, external_category_id);


--
-- Name: IX_external_category_attributes_marketplace_code_external_cat~1; Type: INDEX; Schema: channels; Owner: -
--

CREATE UNIQUE INDEX "IX_external_category_attributes_marketplace_code_external_cat~1" ON channels.external_category_attributes USING btree (marketplace_code, external_category_id, external_attribute_id);


--
-- Name: IX_marketplace_connections_tenant_id_marketplace_code; Type: INDEX; Schema: channels; Owner: -
--

CREATE UNIQUE INDEX "IX_marketplace_connections_tenant_id_marketplace_code" ON channels.marketplace_connections USING btree (tenant_id, marketplace_code);


--
-- Name: IX_product_import_run_errors_product_import_run_id; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_product_import_run_errors_product_import_run_id" ON channels.product_import_run_errors USING btree (product_import_run_id);


--
-- Name: IX_product_import_runs_status_created_at; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_product_import_runs_status_created_at" ON channels.product_import_runs USING btree (status, created_at);


--
-- Name: IX_product_import_runs_tenant_id_marketplace_code_created_at; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_product_import_runs_tenant_id_marketplace_code_created_at" ON channels.product_import_runs USING btree (tenant_id, marketplace_code, created_at DESC);


--
-- Name: IX_product_listings_product_item_id; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_product_listings_product_item_id" ON channels.product_listings USING btree (product_item_id);


--
-- Name: IX_product_listings_tenant_id_marketplace_code_product_item_id; Type: INDEX; Schema: channels; Owner: -
--

CREATE UNIQUE INDEX "IX_product_listings_tenant_id_marketplace_code_product_item_id" ON channels.product_listings USING btree (tenant_id, marketplace_code, product_item_id);


--
-- Name: IX_product_publication_run_errors_product_publication_run_id; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_product_publication_run_errors_product_publication_run_id" ON channels.product_publication_run_errors USING btree (product_publication_run_id);


--
-- Name: IX_product_publication_runs_status_created_at; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_product_publication_runs_status_created_at" ON channels.product_publication_runs USING btree (status, created_at);


--
-- Name: IX_product_publication_runs_tenant_id_marketplace_code_created~; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_product_publication_runs_tenant_id_marketplace_code_created~" ON channels.product_publication_runs USING btree (tenant_id, marketplace_code, created_at DESC);


--
-- Name: IX_taxonomy_sync_runs_marketplace_code; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_taxonomy_sync_runs_marketplace_code" ON channels.taxonomy_sync_runs USING btree (marketplace_code);


--
-- Name: IX_taxonomy_sync_runs_marketplace_code_status; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX "IX_taxonomy_sync_runs_marketplace_code_status" ON channels.taxonomy_sync_runs USING btree (marketplace_code, status);


--
-- Name: ix_product_listings_dirty; Type: INDEX; Schema: channels; Owner: -
--

CREATE INDEX ix_product_listings_dirty ON channels.product_listings USING btree (tenant_id, marketplace_code) WHERE ((content_dirty_at IS NOT NULL) OR (offer_dirty_at IS NOT NULL));


--
-- Name: product_import_run_errors FK_product_import_run_errors_product_import_runs_product_impor~; Type: FK CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.product_import_run_errors
    ADD CONSTRAINT "FK_product_import_run_errors_product_import_runs_product_impor~" FOREIGN KEY (product_import_run_id) REFERENCES channels.product_import_runs(id) ON DELETE CASCADE;


--
-- Name: product_publication_run_errors FK_product_publication_run_errors_product_publication_runs_pro~; Type: FK CONSTRAINT; Schema: channels; Owner: -
--

ALTER TABLE ONLY channels.product_publication_run_errors
    ADD CONSTRAINT "FK_product_publication_run_errors_product_publication_runs_pro~" FOREIGN KEY (product_publication_run_id) REFERENCES channels.product_publication_runs(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--


