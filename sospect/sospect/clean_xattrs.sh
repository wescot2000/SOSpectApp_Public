#!/bin/bash
# Limpia todos los atributos extendidos del app bundle excepto com.apple.provenance
APP_DIR="$1"
if [ -z "$APP_DIR" ] || [ ! -d "$APP_DIR" ]; then
    echo "No app bundle directory provided or not found: $APP_DIR"
    exit 0
fi

# Usar process substitution en vez de pipe para evitar subshell
while IFS= read -r -d '' file; do
    while IFS= read -r attr; do
        if [ -n "$attr" ] && [ "$attr" != "com.apple.provenance" ]; then
            xattr -d "$attr" "$file" 2>/dev/null
        fi
    done < <(xattr "$file" 2>/dev/null)
done < <(find "$APP_DIR" -print0)

# Eliminar resource forks y archivos basura
find "$APP_DIR" -name '._*' -delete 2>/dev/null
find "$APP_DIR" -name '.DS_Store' -delete 2>/dev/null

echo "Cleaned all removable extended attributes from $APP_DIR"
