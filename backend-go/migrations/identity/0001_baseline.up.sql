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
-- Name: identity; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA IF NOT EXISTS identity;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: __ef_migrations_history; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.__ef_migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: tenant_memberships; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.tenant_memberships (
    id uuid NOT NULL,
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    is_primary boolean NOT NULL,
    joined_at timestamp with time zone NOT NULL
);


--
-- Name: tenants; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.tenants (
    id uuid NOT NULL,
    name character varying(200) NOT NULL,
    created_at timestamp with time zone NOT NULL
);


--
-- Name: users; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.users (
    id uuid NOT NULL,
    email character varying(320) NOT NULL,
    password_hash text NOT NULL,
    name character varying(200) DEFAULT ''::character varying NOT NULL,
    created_at timestamp with time zone NOT NULL
);


--
-- Name: __ef_migrations_history PK___ef_migrations_history; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.__ef_migrations_history
    ADD CONSTRAINT "PK___ef_migrations_history" PRIMARY KEY ("MigrationId");


--
-- Name: tenant_memberships PK_tenant_memberships; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.tenant_memberships
    ADD CONSTRAINT "PK_tenant_memberships" PRIMARY KEY (id);


--
-- Name: tenants PK_tenants; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.tenants
    ADD CONSTRAINT "PK_tenants" PRIMARY KEY (id);


--
-- Name: users PK_users; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.users
    ADD CONSTRAINT "PK_users" PRIMARY KEY (id);


--
-- Name: IX_tenant_memberships_tenant_id_user_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_tenant_memberships_tenant_id_user_id" ON identity.tenant_memberships USING btree (tenant_id, user_id);


--
-- Name: IX_tenant_memberships_user_id; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_tenant_memberships_user_id" ON identity.tenant_memberships USING btree (user_id);


--
-- Name: IX_tenant_memberships_user_id_is_primary; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX "IX_tenant_memberships_user_id_is_primary" ON identity.tenant_memberships USING btree (user_id, is_primary);


--
-- Name: IX_users_email; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX "IX_users_email" ON identity.users USING btree (email);


--
-- PostgreSQL database dump complete
--


