#!/opt/homebrew/bin/bash

# Exit immediately if a command exits with a non-zero status (including in the middle of a pipeline).
set -e 
set -o pipefail

script_dir=$(dirname "${BASH_SOURCE[0]}")
source "$script_dir/../../global_utils.sh"
source "$script_dir/../az_setup_utils.sh"

# -------------------------------------------------------------------------------------------------------

# ToDo: For Staging, add checks here and options below!
env_var_is_set "DEV_CHECKMADE_SUBMISSIONS_BOT_TOKEN" "secret"
env_var_is_set "DEV_CHECKMADE_COMMUNICATIONS_BOT_TOKEN" "secret"
env_var_is_set "DEV_CHECKMADE_NOTIFICATIONS_BOT_TOKEN" "secret"
env_var_is_set "PRD_CHECKMADE_SUBMISSIONS_BOT_TOKEN" "secret"
env_var_is_set "PRD_CHECKMADE_COMMUNICATIONS_BOT_TOKEN" "secret"
env_var_is_set "PRD_CHECKMADE_NOTIFICATIONS_BOT_TOKEN" "secret"

echo "--- Welcome to the Telegram Bot Setup Tool ---"
echo "Here you can manage the WebHook of an EXISTING Telegram Bot (created with BotFather) as found in ENVIRONMENT"

echo "Please choose the bot by entering the two-digit id:"
echo "ds = (dev) Submissions Bot"
echo "dc = (dev) Communications Bot"
echo "dn = (dev) Notifications Bot"
echo "ps = (prd) Submissions Bot"
echo "pc = (prd) Communications Bot"
echo "pn = (prd) Notifications Bot"

read -r bot_choice

if [ -z "$bot_choice" ]; then
  echo "No bot was chosen, aborting"
  exit 0
elif [ "$bot_choice" == "ds" ]; then
  bot_token="$DEV_CHECKMADE_SUBMISSIONS_BOT_TOKEN"
elif [ "$bot_choice" == "dc" ]; then
  bot_token="$DEV_CHECKMADE_COMMUNICATIONS_BOT_TOKEN"
elif [ "$bot_choice" == "dn" ]; then
  bot_token="$DEV_CHECKMADE_NOTIFICATIONS_BOT_TOKEN"
elif [ "$bot_choice" == "ps" ]; then
  bot_token="$PRD_CHECKMADE_SUBMISSIONS_BOT_TOKEN"
elif [ "$bot_choice" == "pc" ]; then
  bot_token="$PRD_CHECKMADE_COMMUNICATIONS_BOT_TOKEN"
elif [ "$bot_choice" == "pn" ]; then
  bot_token="$PRD_CHECKMADE_NOTIFICATIONS_BOT_TOKEN"
else
  echo "Err: No valid bot choice, aborting"
  exit 1
fi

echo "What would you like to do? Set WebHook (default behaviour, continue with 'Enter') or \
get current WebhookInfo (enter 'g')?"
read -r bot_setup_task

if [ "$bot_setup_task" == "g" ]; then
  curl --request POST --url https://api.telegram.org/bot"$bot_token"/getWebhookInfo
  exit 0
fi

bot_type=${bot_choice:1:1} # the second letter

if [ "$bot_type" == "s" ]; then
  function_name="SubmissionsBot"
elif [ "$bot_type" == "c" ]; then
  function_name="CommunicationsBot"
elif [ "$bot_type" == "n" ]; then
  function_name="NotificationsBot"
fi

bot_hosting_context=${bot_choice:0:1} # the first letter

if [ "$bot_hosting_context" == "d" ]; then

  echo "Please enter the https function endpoint host (use 'ngrok http 7071' in a separate CLI instance to generate \
the URL that forwards to localhost)"
  read -r functionapp_endpoint
  functionapp_endpoint="$functionapp_endpoint/api/${function_name,,}" # ,, = to lower case
  
elif [ "$bot_hosting_context" == "p" ]; then 

  echo "Select functionapp to connect to Telegram..."
  FUNCTIONAPP_NAME=$(confirm_and_select_resource "functionapp" "$FUNCTIONAPP_NAME")
  functionapp_endpoint="https://$FUNCTIONAPP_NAME.azurewebsites.net"
  functionapp_endpoint="$functionapp_endpoint/api/${function_name,,}" # ,, = to lower case
  
  function_code=$(az functionapp function keys list \
  -n "$FUNCTIONAPP_NAME" --function-name "$function_name" \
  --query default --output tsv)
  
  functionapp_endpoint="$functionapp_endpoint?code=$function_code"
fi

echo "FYI your function endpoint with gateway is:"
echo "$functionapp_endpoint"
echo "Now setting Webhook..."

curl --request POST \
--url https://api.telegram.org/bot"$bot_token"/setWebhook \
--header 'content-type: application/json' \
--data '{"url": "'"$functionapp_endpoint"'"}'

