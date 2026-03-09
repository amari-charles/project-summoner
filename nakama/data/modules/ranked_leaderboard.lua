local nk = require("nakama")

local ok, err = pcall(function()
  nk.leaderboard_create("ranked_1v1", false, "desc", "best", "", {}, true)
end)

if ok then
  nk.logger_info("[ranked_e2e] ensured leaderboard ranked_1v1")
else
  local message = tostring(err)
  if string.find(string.lower(message), "already") then
    nk.logger_info("[ranked_e2e] leaderboard ranked_1v1 already exists")
  else
    nk.logger_error("[ranked_e2e] failed to create leaderboard ranked_1v1: " .. message)
  end
end
