#!/opt/homebrew/bin/bash

set -e 
set -o pipefail
script_dir=$(dirname "${BASH_SOURCE[0]}")
source "$script_dir/../../global_utils.sh"
source "$script_dir/../az_setup_utils.sh"

# -------------------------------------------------------------------------------------------------------

env_var_is_set "PG_DB_NAME"
env_var_is_set "PG_APP_USER"
env_var_is_set "COSMOSDB_HOST"

echo "Choose the functionapp to which the Connection String shall be added:"
FUNCTIONAPP_NAME=$(confirm_and_select_resource "functionapp" "$FUNCTIONAPP_NAME")

echo "Now constructing connection string from its components in ADO.NET format..."
cosmosdb_name="$PG_DB_NAME"
cosmosdb_port="5432"
cosmosdb_user="$PG_APP_USER"
cosmosdb_psw="MYSECRET" # Will be replaced with value from KeyVault in Program/Startup.cs
cosmosdb_options="Ssl Mode=Require;Trust Server Certificate=true;Include Error Detail=true"

cosmosdb_connstring="Server=$COSMOSDB_HOST;Database=$cosmosdb_name;Port=$cosmosdb_port;User Id=$cosmosdb_user;\
Password=$cosmosdb_psw;$cosmosdb_options"
echo "$cosmosdb_connstring"

echo "Enter the key for the Connection String (e.g. PrdDb):"
read -r connstring_key
connstring_settings="$connstring_key='$cosmosdb_connstring'"

echo "Now setting Connection String in '${FUNCTIONAPP_NAME}'"
set -x
az webapp config connection-string set --name "$FUNCTIONAPP_NAME" --connection-string-type PostgreSQL \
--settings "$connstring_settings"