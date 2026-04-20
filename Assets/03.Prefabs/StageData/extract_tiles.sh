#!/bin/bash

extract_tiles() {
    local file=$1
    local stage_name=$(basename "$file" .prefab)
    
    # Extract all positions and tile types
    local lines=$(cat "$file")
    local in_prefab=0
    local curr_x=""
    local curr_y=""
    local curr_type=""
    
    # Use grep to find all m_LocalPosition.x and m_LocalPosition.y and manualTileType patterns
    echo "=== $stage_name ==="
    
    # Extract lines with positions and types
    grep -n "m_LocalPosition\.\|manualTileType" "$file" | while IFS=: read -r linenum content; do
        if [[ "$content" =~ "m_LocalPosition.x" ]]; then
            # Next line should have the value
            next_line=$((linenum + 1))
            sed -n "${next_line}p" "$file" | grep -oE "value: [-0-9.]+$" | sed 's/value: //'
        fi
    done
}

for f in "Stage 1"/*.prefab "Stage 2"/*.prefab; do
    [ -f "$f" ] && extract_tiles "$f" 2>/dev/null | head -20
done
