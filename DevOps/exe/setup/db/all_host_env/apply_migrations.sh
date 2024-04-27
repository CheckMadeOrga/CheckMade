#!/opt/homebrew/bin/bash

set -e 
set -o pipefail
script_dir=$(dirname "${BASH_SOURCE[0]}")
source "$script_dir/../../../global_utils.sh"
source "$script_dir/../../db_utils.sh"

# -------------------------------------------------------------------------------------------------------
# Script works across all hosting environments!

hosting_env="$1"
hosting_env_is_valid "$1"

env_var_is_set "PG_SUPER_USER"
env_var_is_set "PG_DB_NAME"

if [ "$hosting_env" != "CI" ]; then
  echo "Apply all migrations to recreate database (y/n)?"
  read -r confirm_ops_setup
  if [ "$confirm_ops_setup" != "y" ]; then
    echo "Aborting"
    exit 0
  fi
fi

# Only needs to be set via Environment Vars in 'CI' because lack of interactivity there (e.g. no psw prompt possible)
if [ "$hosting_env" == "CI" ]; then
  env_var_is_set "PGPASSWORD" "secret"
fi

psql_host=$(get_psql_host "$hosting_env")

migrations_dir="$script_dir/../../../../sql/migrations"
for sql_file in $(ls $migrations_dir/*.sql | sort); do
  echo "Applying migration: $sql_file"
  psql -h "$psql_host" -U "$PG_SUPER_USER" -d "$PG_DB_NAME" -f "$sql_file"
  if [ $? -ne 0 ]; then
    echo "Error applying migration: $sql_file"
    exit 1
  fi
done
echo "All migrations applied successfully."  
