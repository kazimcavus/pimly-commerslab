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
-- Name: catalog; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA IF NOT EXISTS catalog;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: __ef_migrations_history; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: attribute_values; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.attribute_values (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    attribute_id uuid NOT NULL
);


--
-- Name: attributes; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.attributes (
    id uuid NOT NULL,
    key character varying(200) NOT NULL,
    name character varying(500) NOT NULL,
    tenant_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


--
-- Name: barcode_allocations; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.barcode_allocations (
    id uuid NOT NULL,
    barcode character varying(200) NOT NULL,
    allocated_at timestamp with time zone NOT NULL,
    tenant_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


--
-- Name: barcode_sequence; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.barcode_sequence (
    id integer NOT NULL,
    next_value bigint NOT NULL,
    client_allocation_required boolean DEFAULT false NOT NULL,
    tenant_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


--
-- Name: brands; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.brands (
    id uuid NOT NULL,
    name character varying(500) NOT NULL,
    code character varying(100),
    tenant_id uuid NOT NULL
);


--
-- Name: catalog_settings; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.catalog_settings (
    id integer NOT NULL,
    tenant_id uuid NOT NULL,
    slicer_name_position character varying(10) NOT NULL
);


--
-- Name: categories; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.categories (
    id uuid NOT NULL,
    name character varying(500) NOT NULL,
    code character varying(100),
    parent_id uuid,
    tenant_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


--
-- Name: category_attributes; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.category_attributes (
    id uuid NOT NULL,
    attribute_id uuid NOT NULL,
    required boolean NOT NULL,
    sort_order integer NOT NULL,
    category_id uuid NOT NULL,
    scope character varying(20) DEFAULT 'model'::character varying NOT NULL
);


--
-- Name: outbox_messages; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.outbox_messages (
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
-- Name: product_images; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.product_images (
    id uuid NOT NULL,
    url character varying(2000) NOT NULL,
    sort_order integer NOT NULL,
    alt_text character varying(500),
    is_primary boolean NOT NULL,
    variant_value_id uuid,
    product_id uuid NOT NULL,
    tenant_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


--
-- Name: product_items; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.product_items (
    id uuid NOT NULL,
    sku character varying(200),
    barcode character varying(200) NOT NULL,
    gtin character varying(50),
    mpn character varying(100),
    axis_value_entry_id uuid,
    axis_value character varying(500),
    attribute_values jsonb NOT NULL,
    variant_values jsonb NOT NULL,
    product_id uuid NOT NULL,
    tenant_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


--
-- Name: products; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.products (
    id uuid NOT NULL,
    group_id uuid NOT NULL,
    product_sku character varying(200) NOT NULL,
    title character varying(500) NOT NULL,
    status character varying(20) NOT NULL,
    attribute_values jsonb NOT NULL,
    variants jsonb NOT NULL,
    tenant_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    category_id uuid NOT NULL,
    group_code character varying(200),
    slicer_value character varying(200),
    brand_id uuid,
    description text
);


--
-- Name: sku_generator_config; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.sku_generator_config (
    id integer NOT NULL,
    enabled boolean NOT NULL,
    counter_next_value bigint NOT NULL,
    segments jsonb NOT NULL,
    tenant_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


--
-- Name: variant_values; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.variant_values (
    id uuid NOT NULL,
    label character varying(200) NOT NULL,
    color character varying(50),
    image_url character varying(2000),
    sort_order integer NOT NULL,
    variant_id uuid NOT NULL,
    key character varying(200) NOT NULL
);


--
-- Name: variants; Type: TABLE; Schema: catalog; Owner: -
--

CREATE TABLE catalog.variants (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    selection_style character varying(20) NOT NULL,
    sort_order integer NOT NULL,
    slicer boolean DEFAULT false NOT NULL,
    key character varying(200) NOT NULL,
    tenant_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL
);


--
-- Name: __ef_migrations_history PK___ef_migrations_history; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.__ef_migrations_history
    ADD CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId");


--
-- Name: attribute_values PK_attribute_values; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.attribute_values
    ADD CONSTRAINT "PK_attribute_values" PRIMARY KEY (id);


--
-- Name: attributes PK_attributes; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.attributes
    ADD CONSTRAINT "PK_attributes" PRIMARY KEY (id);


--
-- Name: barcode_allocations PK_barcode_allocations; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.barcode_allocations
    ADD CONSTRAINT "PK_barcode_allocations" PRIMARY KEY (id);


--
-- Name: barcode_sequence PK_barcode_sequence; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.barcode_sequence
    ADD CONSTRAINT "PK_barcode_sequence" PRIMARY KEY (tenant_id, id);


--
-- Name: brands PK_brands; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.brands
    ADD CONSTRAINT "PK_brands" PRIMARY KEY (id);


--
-- Name: catalog_settings PK_catalog_settings; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.catalog_settings
    ADD CONSTRAINT "PK_catalog_settings" PRIMARY KEY (tenant_id, id);


--
-- Name: categories PK_categories; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.categories
    ADD CONSTRAINT "PK_categories" PRIMARY KEY (id);


--
-- Name: category_attributes PK_category_attributes; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.category_attributes
    ADD CONSTRAINT "PK_category_attributes" PRIMARY KEY (id);


--
-- Name: outbox_messages PK_outbox_messages; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.outbox_messages
    ADD CONSTRAINT "PK_outbox_messages" PRIMARY KEY (id);


--
-- Name: product_images PK_product_images; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.product_images
    ADD CONSTRAINT "PK_product_images" PRIMARY KEY (id);


--
-- Name: product_items PK_product_items; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.product_items
    ADD CONSTRAINT "PK_product_items" PRIMARY KEY (id);


--
-- Name: products PK_products; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.products
    ADD CONSTRAINT "PK_products" PRIMARY KEY (id);


--
-- Name: sku_generator_config PK_sku_generator_config; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.sku_generator_config
    ADD CONSTRAINT "PK_sku_generator_config" PRIMARY KEY (tenant_id, id);


--
-- Name: variant_values PK_variant_values; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.variant_values
    ADD CONSTRAINT "PK_variant_values" PRIMARY KEY (id);


--
-- Name: variants PK_variants; Type: CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.variants
    ADD CONSTRAINT "PK_variants" PRIMARY KEY (id);


--
-- Name: IX_attribute_values_attribute_id_name; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_attribute_values_attribute_id_name" ON catalog.attribute_values USING btree (attribute_id, name);


--
-- Name: IX_attributes_tenant_id_key; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_attributes_tenant_id_key" ON catalog.attributes USING btree (tenant_id, key);


--
-- Name: IX_barcode_allocations_tenant_id_barcode; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_barcode_allocations_tenant_id_barcode" ON catalog.barcode_allocations USING btree (tenant_id, barcode);


--
-- Name: IX_brands_tenant_id_name; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_brands_tenant_id_name" ON catalog.brands USING btree (tenant_id, name);


--
-- Name: IX_categories_parent_id; Type: INDEX; Schema: catalog; Owner: -
--

CREATE INDEX "IX_categories_parent_id" ON catalog.categories USING btree (parent_id);


--
-- Name: IX_category_attributes_attribute_id; Type: INDEX; Schema: catalog; Owner: -
--

CREATE INDEX "IX_category_attributes_attribute_id" ON catalog.category_attributes USING btree (attribute_id);


--
-- Name: IX_category_attributes_category_id_attribute_id; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_category_attributes_category_id_attribute_id" ON catalog.category_attributes USING btree (category_id, attribute_id);


--
-- Name: IX_outbox_messages_processed_on_utc_occurred_on_utc; Type: INDEX; Schema: catalog; Owner: -
--

CREATE INDEX "IX_outbox_messages_processed_on_utc_occurred_on_utc" ON catalog.outbox_messages USING btree (processed_on_utc, occurred_on_utc);


--
-- Name: IX_product_images_product_id; Type: INDEX; Schema: catalog; Owner: -
--

CREATE INDEX "IX_product_images_product_id" ON catalog.product_images USING btree (product_id);


--
-- Name: IX_product_items_product_id; Type: INDEX; Schema: catalog; Owner: -
--

CREATE INDEX "IX_product_items_product_id" ON catalog.product_items USING btree (product_id);


--
-- Name: IX_product_items_tenant_id_barcode; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_product_items_tenant_id_barcode" ON catalog.product_items USING btree (tenant_id, barcode);


--
-- Name: IX_product_items_tenant_id_sku; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_product_items_tenant_id_sku" ON catalog.product_items USING btree (tenant_id, sku) WHERE (sku IS NOT NULL);


--
-- Name: IX_products_brand_id; Type: INDEX; Schema: catalog; Owner: -
--

CREATE INDEX "IX_products_brand_id" ON catalog.products USING btree (brand_id);


--
-- Name: IX_products_category_id; Type: INDEX; Schema: catalog; Owner: -
--

CREATE INDEX "IX_products_category_id" ON catalog.products USING btree (category_id);


--
-- Name: IX_products_tenant_id_group_code; Type: INDEX; Schema: catalog; Owner: -
--

CREATE INDEX "IX_products_tenant_id_group_code" ON catalog.products USING btree (tenant_id, group_code);


--
-- Name: IX_products_tenant_id_product_sku; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_products_tenant_id_product_sku" ON catalog.products USING btree (tenant_id, product_sku);


--
-- Name: IX_variant_values_variant_id_key; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_variant_values_variant_id_key" ON catalog.variant_values USING btree (variant_id, key);


--
-- Name: IX_variant_values_variant_id_label; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_variant_values_variant_id_label" ON catalog.variant_values USING btree (variant_id, label);


--
-- Name: IX_variants_tenant_id_key; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_variants_tenant_id_key" ON catalog.variants USING btree (tenant_id, key);


--
-- Name: IX_variants_tenant_id_name; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_variants_tenant_id_name" ON catalog.variants USING btree (tenant_id, name);


--
-- Name: IX_variants_tenant_id_slicer; Type: INDEX; Schema: catalog; Owner: -
--

CREATE UNIQUE INDEX "IX_variants_tenant_id_slicer" ON catalog.variants USING btree (tenant_id, slicer) WHERE (slicer = true);


--
-- Name: attribute_values FK_attribute_values_attributes_attribute_id; Type: FK CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.attribute_values
    ADD CONSTRAINT "FK_attribute_values_attributes_attribute_id" FOREIGN KEY (attribute_id) REFERENCES catalog.attributes(id) ON DELETE CASCADE;


--
-- Name: categories FK_categories_categories_parent_id; Type: FK CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.categories
    ADD CONSTRAINT "FK_categories_categories_parent_id" FOREIGN KEY (parent_id) REFERENCES catalog.categories(id) ON DELETE SET NULL;


--
-- Name: category_attributes FK_category_attributes_categories_category_id; Type: FK CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.category_attributes
    ADD CONSTRAINT "FK_category_attributes_categories_category_id" FOREIGN KEY (category_id) REFERENCES catalog.categories(id) ON DELETE CASCADE;


--
-- Name: product_images FK_product_images_products_product_id; Type: FK CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.product_images
    ADD CONSTRAINT "FK_product_images_products_product_id" FOREIGN KEY (product_id) REFERENCES catalog.products(id) ON DELETE CASCADE;


--
-- Name: product_items FK_product_items_products_product_id; Type: FK CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.product_items
    ADD CONSTRAINT "FK_product_items_products_product_id" FOREIGN KEY (product_id) REFERENCES catalog.products(id) ON DELETE CASCADE;


--
-- Name: products FK_products_brands_brand_id; Type: FK CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.products
    ADD CONSTRAINT "FK_products_brands_brand_id" FOREIGN KEY (brand_id) REFERENCES catalog.brands(id) ON DELETE SET NULL;


--
-- Name: products FK_products_categories_category_id; Type: FK CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.products
    ADD CONSTRAINT "FK_products_categories_category_id" FOREIGN KEY (category_id) REFERENCES catalog.categories(id) ON DELETE RESTRICT;


--
-- Name: variant_values FK_variant_values_variants_variant_id; Type: FK CONSTRAINT; Schema: catalog; Owner: -
--

ALTER TABLE ONLY catalog.variant_values
    ADD CONSTRAINT "FK_variant_values_variants_variant_id" FOREIGN KEY (variant_id) REFERENCES catalog.variants(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--


