DO $$
    DECLARE
        userName CONSTANT text := 'cmappuser';
        tableName CONSTANT text := 'spheres_of_action';
    BEGIN
        EXECUTE FORMAT(
                'CREATE TABLE IF NOT EXISTS %I (
                    id SERIAL PRIMARY KEY,
                    name varchar(255) NOT NULL,
                    trade varchar(6) NOT NULL,
                    live_event_id INT NOT NULL REFERENCES live_events(id),
                    details JSONB NOT NULL,
                    status SMALLINT NOT NULL,
                    last_data_migration SMALLINT)',
                tableName);

        EXECUTE FORMAT('GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE %I TO %I', tableName, userName);
        EXECUTE FORMAT('GRANT USAGE, SELECT, UPDATE ON SEQUENCE %I_id_seq TO %I', tableName, userName);
    END
$$;

CREATE UNIQUE INDEX spheres_of_action_live_event_name ON spheres_of_action (live_event_id, name);