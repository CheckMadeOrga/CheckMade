#!/opt/homebrew/bin/bash

set -e 
set -o pipefail
script_dir=$(dirname "${BASH_SOURCE[0]}")
source "$script_dir/../../global_utils.sh"
source "$script_dir/../az_setup_utils.sh"

# -------------------------------------------------------------------------------------------------------

env_var_is_set "PG_APP_USER"
env_var_is_set "PG_APP_USER_PSW" "secret"

echo "Choose the keyvault to which the password for DB user '$PG_APP_USER' shall be saved"
KEYVAULT_NAME=$(confirm_and_select_resource "keyvault" "$KEYVAULT_NAME")

echo "Enter the key for the new secret (e.g. 'PrdDbPsw'):"
read -r secret_key

secret_key="ConnectionStrings--$secret_key"

echo "Now setting a new secret in keyvault with the password for the db app user (which is stored in the environment)"
az keyvault secret set --vault-name "$KEYVAULT_NAME" --name "$secret_key" --value "$PG_APP_USER_PSW"
