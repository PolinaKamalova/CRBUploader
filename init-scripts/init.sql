CREATE TABLE public.quotations (
	id varchar(255) NULL,
	charcode varchar(255) NULL,
	"name" varchar(255) NULL,
	value float8 NULL,
	numcode int4 NULL,
	"date" varchar(255) NULL
);
CREATE INDEX quotations_id_idx ON public.quotations (id);
