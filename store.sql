--
-- PostgreSQL database dump
--

-- Dumped from database version 16.6
-- Dumped by pg_dump version 16.6

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: add_order(text, text, integer, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.add_order(user_tg_id text, username text, prod_id integer, prod_count integer) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
	client_id numeric;
	remains numeric;
    BEGIN
		client_id = (select get_client(user_tg_id, username));
		remains = (select pr.remains from product_remains() pr where prod_id = pr.product_id limit 1);
		IF remains < prod_count THEN
			RETURN false;
		END IF;
		INSERT INTO orders(product_id, user_id, product_count)
		values(prod_id, client_id, prod_count);
		RETURN true;
    END;
$$;


ALTER FUNCTION public.add_order(user_tg_id text, username text, prod_id integer, prod_count integer) OWNER TO postgres;

--
-- Name: cancel_order(text, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.cancel_order(user_tg_id text, prod_id integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
    BEGIN
		DELETE FROM orders o
		 WHERE product_id = prod_id
		   AND user_id IN (SELECT id FROM clients WHERE client_tg_id = user_tg_id);
    END;
$$;


ALTER FUNCTION public.cancel_order(user_tg_id text, prod_id integer) OWNER TO postgres;

--
-- Name: close_order(text, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.close_order(user_tg_id text, prod_id integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
    BEGIN
WITH a AS (
	    SELECT product_id, SUM(product_count)
	      FROM orders o
		 WHERE (product_id = prod_id or prod_id = -1)
		   AND user_id IN (SELECT id FROM clients WHERE client_tg_id = user_tg_id)
		 GROUP BY product_id)
		UPDATE products p
		   SET product_count = coalesce(product_count, 0) - (SELECT sum FROM a WHERE p.id = product_id)
		 WHERE id IN (SELECT product_id FROM a WHERE p.id = product_id);
		 
		DELETE FROM orders o
		 WHERE (product_id = prod_id or prod_id = -1)
		   AND user_id IN (SELECT id FROM clients WHERE client_tg_id = user_tg_id);
    END;
$$;


ALTER FUNCTION public.close_order(user_tg_id text, prod_id integer) OWNER TO postgres;

--
-- Name: get_client(text, text); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.get_client(user_tg_id text, username text) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
	client_id integer;
    BEGIN
		client_id = (select id from clients c where c.client_tg_id  = user_tg_id);
		IF client_id IS NULL THEN
        	INSERT INTO clients(username, client_tg_id)
			VALUES(username, user_tg_id)
			RETURNING id INTO client_id;
		END IF;
		RETURN client_id;
    END;
$$;


ALTER FUNCTION public.get_client(user_tg_id text, username text) OWNER TO postgres;

--
-- Name: product_remains(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.product_remains() RETURNS TABLE(product_id integer, product_name text, remains numeric)
    LANGUAGE plpgsql
    AS $$
    BEGIN
         RETURN QUERY
			SELECT p.id, p.product_name::text as product_name, (p.product_count - SUM(coalesce(o.product_count, 0)))::numeric from orders o
			join clients c on c.id = o.user_id
			right join products p on p.id = o.product_id
			group by p.id, p.product_name, p.product_count;
    END;
$$;


ALTER FUNCTION public.product_remains() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: clients; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.clients (
    id integer NOT NULL,
    username text NOT NULL,
    client_tg_id text
);


ALTER TABLE public.clients OWNER TO postgres;

--
-- Name: clients_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.clients_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.clients_id_seq OWNER TO postgres;

--
-- Name: clients_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.clients_id_seq OWNED BY public.clients.id;


--
-- Name: orders; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.orders (
    id integer NOT NULL,
    product_id integer NOT NULL,
    user_id integer NOT NULL,
    product_count integer
);


ALTER TABLE public.orders OWNER TO postgres;

--
-- Name: orders_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.orders_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.orders_id_seq OWNER TO postgres;

--
-- Name: orders_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.orders_id_seq OWNED BY public.orders.id;


--
-- Name: products; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.products (
    id integer NOT NULL,
    product_name text NOT NULL,
    product_count integer NOT NULL
);


ALTER TABLE public.products OWNER TO postgres;

--
-- Name: products_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.products_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.products_id_seq OWNER TO postgres;

--
-- Name: products_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.products_id_seq OWNED BY public.products.id;


--
-- Name: user_admin; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.user_admin (
    id integer NOT NULL,
    username text NOT NULL,
    chat_id bigint NOT NULL
);


ALTER TABLE public.user_admin OWNER TO postgres;

--
-- Name: user_admin_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.user_admin_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.user_admin_id_seq OWNER TO postgres;

--
-- Name: user_admin_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.user_admin_id_seq OWNED BY public.user_admin.id;


--
-- Name: v_orders; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_orders AS
 SELECT c.id AS client_id,
    p.id AS product_id,
    c.username,
    c.client_tg_id,
    p.product_name,
    sum(o.product_count) AS sum
   FROM ((public.orders o
     JOIN public.clients c ON ((c.id = o.user_id)))
     JOIN public.products p ON ((p.id = o.product_id)))
  GROUP BY c.id, c.username, c.client_tg_id, p.id, p.product_name;


ALTER VIEW public.v_orders OWNER TO postgres;

--
-- Name: clients id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clients ALTER COLUMN id SET DEFAULT nextval('public.clients_id_seq'::regclass);


--
-- Name: orders id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders ALTER COLUMN id SET DEFAULT nextval('public.orders_id_seq'::regclass);


--
-- Name: products id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.products ALTER COLUMN id SET DEFAULT nextval('public.products_id_seq'::regclass);


--
-- Name: user_admin id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.user_admin ALTER COLUMN id SET DEFAULT nextval('public.user_admin_id_seq'::regclass);


--
-- Data for Name: clients; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.clients (id, username, client_tg_id) FROM stdin;
11	gjhjk	gjkhjk
12	gjhghj	hjkhkj
13	gjhghj	jhjk
14	Jamschotik 	Artoorcheck
15	Александр Дорогов	bboy_dorogov
16	Artooorcheck 	Artooorcheck
\.


--
-- Data for Name: orders; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.orders (id, product_id, user_id, product_count) FROM stdin;
26	1	15	10
38	2	16	4
40	3	16	4
42	4	14	3
43	4	14	2
\.


--
-- Data for Name: products; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.products (id, product_name, product_count) FROM stdin;
1	1kg	15
4	4kg	5
2	2kg	4
3	3kg	4
5	Пряний самодельный	0
6	На вкус запредельный	-5
\.


--
-- Data for Name: user_admin; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.user_admin (id, username, chat_id) FROM stdin;
1	afsffads	34
4	Artooorcheck	770763482
14	Artoorcheck	5654092350
\.


--
-- Name: clients_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.clients_id_seq', 16, true);


--
-- Name: orders_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.orders_id_seq', 44, true);


--
-- Name: products_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.products_id_seq', 6, true);


--
-- Name: user_admin_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.user_admin_id_seq', 14, true);


--
-- Name: user_admin chat_id_unique; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.user_admin
    ADD CONSTRAINT chat_id_unique UNIQUE (chat_id);


--
-- Name: clients clients_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clients
    ADD CONSTRAINT clients_pkey PRIMARY KEY (id);


--
-- Name: orders orders_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT orders_pkey PRIMARY KEY (id);


--
-- Name: products product_name_unique; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT product_name_unique UNIQUE (product_name);


--
-- Name: products products_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT products_pkey PRIMARY KEY (id);


--
-- Name: user_admin user_admin_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.user_admin
    ADD CONSTRAINT user_admin_pkey PRIMARY KEY (id);


--
-- Name: user_admin user_admin_unique; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.user_admin
    ADD CONSTRAINT user_admin_unique UNIQUE (username);


--
-- Name: orders orders_product_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT orders_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id) ON DELETE CASCADE;


--
-- Name: orders orders_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.orders
    ADD CONSTRAINT orders_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.clients(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

