#!/usr/bin/env bash
# PT-01 (NFR-004): staff queue response time with 1,000 requests in the database.
#
# Usage:  bash Test/Performance/PT-01_queue.sh <base-url> [--seed N]
#   <base-url>  a running Development build, e.g. http://localhost:5055
#   --seed N    first submit N requests through the API as the seeded requester
#               (run once with --seed 1000 on a fresh development database)
#
# Workload (Test Plan section 6): 3 warm-up requests, 20 sequential requests,
# then 5 concurrent users for 30 seconds, all on /Staff/Queue as Dev Staff One.
# Target: 95th percentile at or below 3 seconds and zero errors.
# Uses the development users (CR-003 stand-in), so it runs only against a
# Development build. It is not part of the CI gate.
set -u

BASE="${1:?Give the base URL, e.g. http://localhost:5055}"
shift
SEED=0
while [ $# -gt 0 ]; do
  case "$1" in
    --seed) SEED="${2:?--seed needs a number}"; shift 2 ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
done

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
TARGET=3.0

user_id() { grep -o "user=[0-9a-f-]*\">$2<" "$1" | head -1 | sed 's/user=//; s/">.*//'; }

# The development user switcher lists every seeded user except the current one.
curl -s "$BASE/Staff/Queue" -o "$WORK/start.html"
REQUESTER=$(user_id "$WORK/start.html" "Dev Requester")
STAFF_TWO=$(user_id "$WORK/start.html" "Dev Staff Two")
curl -s -c "$WORK/s2.jar" "$BASE/Staff/Queue?user=$STAFF_TWO" -o "$WORK/s2.html"
STAFF_ONE=$(user_id "$WORK/s2.html" "Dev Staff One")
if [ -z "$REQUESTER" ] || [ -z "$STAFF_ONE" ]; then
  echo "Could not find the development users at $BASE. Is it a Development build?" >&2
  exit 1
fi
curl -s -c "$WORK/s1.jar" -o /dev/null "$BASE/Staff/Queue?user=$STAFF_ONE"

if [ "$SEED" -gt 0 ]; then
  curl -s -c "$WORK/rq.jar" -b "$WORK/rq.jar" "$BASE/Requests/Submit?user=$REQUESTER" -o "$WORK/submit.html"
  mapfile -t CATEGORIES < <(grep -o '<option value="[0-9a-f-]*">' "$WORK/submit.html" \
    | sed 's/<option value="//; s/">//' | grep -v '^00000000-0000')
  echo "Seeding $SEED requests across ${#CATEGORIES[@]} categories through POST /api/v1/requests ..."
  for i in $(seq 1 "$SEED"); do
    category="${CATEGORIES[$((i % ${#CATEGORIES[@]}))]}"
    curl -s -o /dev/null -w '%{http_code}\n' -b "$WORK/rq.jar" -H 'Content-Type: application/json' \
      -d "{\"title\":\"PT-01 load request $i\",\"description\":\"Generated for PT-01, not real data.\",\"location\":\"Load test\",\"categoryId\":\"$category\"}" \
      "$BASE/api/v1/requests"
  done | sort | uniq -c | sed 's/^/  HTTP /'
fi

measure() { curl -s -o /dev/null -b "$WORK/s1.jar" -w '%{http_code} %{time_total}\n' "$BASE/Staff/Queue"; }

for i in 1 2 3; do measure > /dev/null; done

for i in $(seq 1 20); do measure; done > "$WORK/sequential.txt"

end=$(( $(date +%s) + 30 ))
for vu in 1 2 3 4 5; do
  ( while [ "$(date +%s)" -lt "$end" ]; do measure; done > "$WORK/vu$vu.txt" ) &
done
wait
cat "$WORK"/vu*.txt > "$WORK/concurrent.txt"

# Prints one summary line and returns 0 when the phase meets the target.
summarise() {
  local file="$1" label="$2"
  awk '{print $2}' "$file" | sort -n > "$WORK/times.txt"
  local n median p95 max errors
  n=$(wc -l < "$WORK/times.txt")
  median=$(awk -v n="$n" 'NR == int((n + 1) / 2)' "$WORK/times.txt")
  p95=$(awk -v n="$n" 'BEGIN { k = int(n * 0.95); if (k < n * 0.95) k++ } NR == k' "$WORK/times.txt")
  max=$(tail -1 "$WORK/times.txt")
  errors=$(awk '$1 != 200' "$file" | wc -l)
  printf '%-34s requests=%-5s median=%ss p95=%ss max=%ss errors=%s\n' \
    "$label" "$n" "$median" "$p95" "$max" "$errors"
  awk -v p="$p95" -v t="$TARGET" -v e="$errors" 'BEGIN { exit !(p <= t && e == 0) }'
}

echo "PT-01 against $BASE at $(date -u '+%Y-%m-%d %H:%M:%SZ'), target p95 <= ${TARGET}s, zero errors"
summarise "$WORK/sequential.txt" "Sequential (20 requests)"; seq_ok=$?
summarise "$WORK/concurrent.txt" "Concurrent (5 users, 30 s)"; con_ok=$?
if [ "$seq_ok" -eq 0 ] && [ "$con_ok" -eq 0 ]; then echo "Result: PASS"; else echo "Result: FAIL"; exit 1; fi
