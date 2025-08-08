#!/usr/bin/env bash
set -euo pipefail

# Minimal MVP simulation against ApiGateway on port 5006
BASE="http://localhost:5006"
ROOM_API="$BASE/api/room"

log() { echo -e "\n=== $*"; }
run() {
  local method="$1" url="$2" body="${3-}"
  echo "-> ${method} ${url}"
  if [[ -n "${body}" ]]; then
    resp=$(curl -s -S -X "$method" "$url" -H 'Content-Type: application/json' --data "$body")
  else
    resp=$(curl -s -S -X "$method" "$url")
  fi
  echo "<- ${resp}"
}

join() {
  local room="$1" id="$2" name="$3"
  run POST "$ROOM_API/$room/join" "{\"playerId\":\"$id\",\"nickname\":\"$name\"}"
}
start() { run POST "$ROOM_API/$1/start"; }
bet0()   { run POST "$ROOM_API/$1/bet"  "{\"playerId\":\"$2\",\"amount\":0}"; }
fold()   { run POST "$ROOM_API/$1/fold" "{\"playerId\":\"$2\"}"; }
state()  { run GET  "$ROOM_API/$1/state"; }

# ---------------- GAME #1 ----------------
log "GAME #1: 6 players; mid-hand folds; go to showdown"
ROOM1="room1"
join "$ROOM1" p1 Alice
join "$ROOM1" p2 Bob
join "$ROOM1" p3 Carol
join "$ROOM1" p4 Dave
join "$ROOM1" p5 Erin
join "$ROOM1" p6 Frank

start "$ROOM1"

# Preflop (all act 0 in join order)
bet0 "$ROOM1" p1; bet0 "$ROOM1" p2; bet0 "$ROOM1" p3; bet0 "$ROOM1" p4; bet0 "$ROOM1" p5; bet0 "$ROOM1" p6
state "$ROOM1"  # expect Phase=Flop

# Flop: p1 0, p2 0, p3 folds on *their turn*, p4 folds on *their turn*, p5 0, p6 0
bet0 "$ROOM1" p1; bet0 "$ROOM1" p2; fold "$ROOM1" p3; fold "$ROOM1" p4; bet0 "$ROOM1" p5; bet0 "$ROOM1" p6
state "$ROOM1"  # expect Phase=Turn

# Turn: p1 0, p2 0, p3/p4 already folded, p5 0, p6 folds on their turn
bet0 "$ROOM1" p1; bet0 "$ROOM1" p2; bet0 "$ROOM1" p5; fold "$ROOM1" p6
state "$ROOM1"  # expect Phase=River

# River: remaining players (p1, p2, p5) act 0
bet0 "$ROOM1" p1; bet0 "$ROOM1" p2; bet0 "$ROOM1" p5
state "$ROOM1"  # expect Phase=Ended (showdown executed server-side)

# ---------------- GAME #2 ----------------
log "GAME #2: 6 players; early folds; end before late streets"
ROOM2="room2"
join "$ROOM2" q1 Alice2
join "$ROOM2" q2 Bob2
join "$ROOM2" q3 Carol2
join "$ROOM2" q4 Dave2
join "$ROOM2" q5 Erin2
join "$ROOM2" q6 Frank2

start "$ROOM2"

# Preflop: q1 0, q2 0, q3 folds on *their turn*, q4 folds, q5 folds, q6 0
bet0 "$ROOM2" q1; bet0 "$ROOM2" q2; fold "$ROOM2" q3; fold "$ROOM2" q4; fold "$ROOM2" q5; bet0 "$ROOM2" q6
state "$ROOM2"  # expect Phase=Flop with three remaining (q1,q2,q6)

# Flop: q1 0, q2 folds on their turn, q6 folds on their turn => q1 last standing -> hand ends
bet0 "$ROOM2" q1; fold "$ROOM2" q2; fold "$ROOM2" q6
state "$ROOM2"  # expect Phase=Ended

log "Done."
