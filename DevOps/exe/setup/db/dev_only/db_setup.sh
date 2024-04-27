#!/opt/homebrew/bin/bash

set -e 
set -o pipefail
script_dir=$(dirname "${BASH_SOURCE[0]}")
source "$script_dir/../../../global_utils.sh"
source "$script_dir/../../db_utils.sh"

# -------------------------------------------------------------------------------------------------------

env_var_is_set "PG_SUPER_USER"
env_var_is_set "PG_APP_USER"
env_var_is_set "PG_DB_NAME"
env_var_is_set "PG_APP_USER_PSW"

echo "----------------------"
echo "This is for DEV environment only!! \
For production environment, db setup happens by default via Cosmos DB cluster setup. \
For CI environment it is set up in the main workflow in the 'Set up PostgreSQL DB and User' (or similar) step!"
echo "----------------------"
echo "Assuming, Postgres was already installed on this machine (e.g. 'brew install postgres@16')!"
psql --version
if [[ $? -ne 0 ]]; then
    echo "Err: PostgreSQL not installed?"
    exit 1
fi
echo "----------------------"
echo "FYI: The default psql user is set to be the current superuser, i.e.: $PG_SUPER_USER"
echo "FYI: psql on dev won't ask for a password because it uses the 'trusted' unix 'local socket'"

db_cluster_path="$HOME/MyPostgresDBs/CheckMade"

confirm_command \
"Initialise Postgres DB Cluster for CheckMade in '${db_cluster_path}' with super-user '${PG_SUPER_USER}' (y/n)?" \
"initdb --pgdata=$db_cluster_path --auth-host=md5 --username=$PG_SUPER_USER"

echo "Next, setting logging config for our postgres db to '${db_cluster_path}/log/' with rotation etc."
log_settings=('#log_destination' '#logging_collector' '#log_directory' '#log_filename' '#log_file_mode' \
'#log_rotation_age' '#log_rotation_size')
for setting in "${log_settings[@]}"; do
  # Uncomment the line with sed
  sed -i "" "/^$setting/s/^#//" "${db_cluster_path}/postgresql.conf"
done

confirm_command \
"Start the database server for '${db_cluster_path}' (y/n)?" \
"pg_ctl -D $db_cluster_path start"

sql_to_create_ops_db="CREATE DATABASE $PG_DB_NAME;"

confirm_command \
"Create the '${PG_DB_NAME}' database now (y/n)?"
"psql -d postgres -c $sql_to_create_ops_db" 

psql -l

confirm_script_launch "$script_dir/db_app_user_setup.sh" "Development"
confirm_script_launch "$script_dir/apply_migrations.sh" "Development"

echo "----------------------"
echo "Next steps:"
echo "- In case of Rider IDE, connect to the DB via the Database Tool Window. Do NOT use the superuser! \
Instead, use the app_db_user. This way, the database explorer will replicate the privileges the app itself will have, \
and we can not accidentally break the database outside of verified DevOps DB scripts."
echo "- Set up the application's access to the local dev DB via a connection string."
