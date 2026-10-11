# AGENTS.md — SOSpect y Wescot

Estas reglas se aplican a los repositorios del ecosistema SOSpect y a las tareas que los afecten. Las skills aportan flujos especializados; no sustituyen estas reglas. Si una regla local más específica es compatible, también se aplica.

## Entorno y repositorios

Antes de operar, identifica sistema operativo, directorio actual, repositorios disponibles, herramientas instaladas y capacidades de red. No deduzcas rutas locales a partir del nombre de una sesión.

- Windows: repos SOSpect en `D:\trabajo\wescotcorp\sospect\`; Docs en `D:\trabajo\wescotcorp\Docs\`; monorepo Angular en `D:\trabajo\wescotcorp\wescot-platform\`.
- Mac Mini: repos y Docs bajo `/Users/wescot/GitHub/`; monorepo Angular en `/Users/wescot/GitHub/wescot-platform/`. No utilizar `/Users/wescot/Documents/GitHub/` (iCloud Drive y problemas de codesign iOS).
- Codex Cloud u otro entorno remoto: utiliza exclusivamente las rutas reales del workspace clonado. No presupongas rutas locales, WireGuard, acceso a AWS, herramientas ni archivos externos.
- En móvil/tablet se puede coordinar y consultar sistemas conectados; editar/correr código requiere un workspace remoto o un equipo con los repos disponibles. Mantén la trazabilidad mediante ticket Jira, rama/PR GitHub y página Confluence.

## Compilación, pruebas y despliegues

- `SOSpectAppMaui`: nunca ejecutar `dotnet build`, `dotnet restore` u otra compilación MAUI; compila William manualmente.
- `SOSpectAPI`: `dotnet build` está permitido. El despliegue siempre lo realiza William manualmente; informar cuando el cambio esté listo para despliegue.
- SQL: nunca desplegar objetos. Informar qué objetos de línea base cambiaron; William los despliega directamente. No crear manuales de despliegue SQL.
- Angular `sospect-public`: orientar, no desplegar. En Angular 17+ el contenido que se publica está en `dist/sospect-public/browser/`; William copia y ejecuta el procedimiento manual.
- Respetar las pruebas que correspondan y estén permitidas por la regla de cada capa. No alterar producción ni el estado de servidores.

## PostgreSQL y AWS

- Las consultas contra bases autorizadas son de solo lectura: exclusivamente `SELECT` (incluyendo consultas de catálogo de solo lectura). No ejecutar DML, DDL ni procedimientos con efectos.
- Nunca almacenar credenciales, claves PEM, tokens ni contraseñas en skills, `AGENTS.md`, prompts, archivos versionados ni salidas. Solicitar el mecanismo de secretos aprobado para el entorno. No imprimir secretos en logs.
- Windows/macOS locales: usar únicamente la conectividad y herramientas autorizadas que estén instaladas. Codex Cloud: no asumir WireGuard ni rutas privadas; si no hay una vía segura autorizada, detener la consulta y ofrecer SQL para que William la ejecute.
- No modificar configuración de red, abrir puertos públicos, relajar reglas de acceso o desplegar objetos SQL. SSH a EC2 es solo para diagnóstico de lectura; no borrar, editar, reiniciar ni desplegar.
- Producción solo puede consultarse mediante credenciales activas, expresamente autorizadas y de solo lectura. No usar credenciales históricas ni asumir que funcionan.

### Parámetros y conectividad PostgreSQL

- Desarrollo: host `sospect-db-dev.cbctshik7qlu.us-east-1.rds.amazonaws.com`, puerto `5432`, base `sospect`, usuario `a1_r3ad0nly_d3v_s0sp3ct`.
- Producción: host `sospect-db.cbctshik7qlu.us-east-1.rds.amazonaws.com`, puerto `5432`, base `sospect`, usuario `a1_r3ad0nly_pr0d_s0sp3ct`. Verificar disponibilidad con una consulta breve y autorizada; no asumir que la credencial funciona.
- Windows local: Git Bash y `/c/Users/wescot/AppData/Local/Programs/pgAdmin 4/runtime/psql.exe`; requiere el WireGuard existente activo.
- Mac Mini local: `/Applications/pgAdmin 4.app/Contents/SharedSupport/psql`; requiere el WireGuard existente activo.
- Codex Cloud no hereda credenciales ni WireGuard locales. Las consultas a RDS privado se ejecutan desde Windows/Mac autorizados; no modificar la red.
- Codex local puede usar `plugin/scripts/sospect-pg.ps1` (Windows) o `plugin/scripts/sospect-pg.sh` (Mac). Recuperan los secretos del Windows Credential Manager o macOS Keychain bajo `SOSpect-RDS-DEV` / `SOSpect-RDS-PROD`, usando como cuenta el usuario de BD indicado arriba. El almacenamiento de Codex es independiente de Claude.
- Los helpers aceptan una única sentencia `SELECT`, la ejecutan en transacción `READ ONLY` y fijan límites de tiempo. Usar consultas acotadas; no consultar tablas completas, llamar funciones con efectos laterales ni ejecutar DDL, DML o procedimientos.

## Sincronización de esquema SOSpectSQL

Al agregar o quitar columnas de una tabla `public`, buscar y actualizar en la misma tarea sus espejos en `migracion.migra_<tabla>` o `wiped` cuando existan. Revisar procedimientos que usen `SELECT *` o `INSERT ... SELECT *`, porque el mapeo puede ser posicional.

Al crear tablas con FK `RESTRICT`/`NO ACTION` hacia `personas` o `alarmas`, comprobar la integración en `wiped.eliminarusuario()` y `migracion.migrar_datos()` para respaldar, migrar y purgar según corresponda. Revisar también las FK `CASCADE` por pérdida de datos no archivados.

En cambios de esquema, William sincroniza manualmente el modelo PowerDesigner antes de su commit. No dar por terminada la tarea sin señalar esa acción pendiente cuando aplique.

## Git y colaboración — reglas vigentes

- Claude y Codex trabajan en ramas separadas y nunca editan simultáneamente el mismo checkout. Antes de cambiar archivos, revisar `git status`, rama actual, diff existente y trabajo ajeno; no sobrescribir cambios del usuario ni de otro agente.
- Asociar el trabajo con un ticket SDT existente o propuesto y documentar la rama/PR en la trazabilidad acordada.
- La política actual de Claude reserva al usuario todos los commits. Codex no hará commit, merge, push, creación/actualización de PR ni actualización externa sin aprobación explícita para esa acción. La política colaborativa que permitiría a los agentes preparar commits es una propuesta separada y no está activa.
- El merge a `main` requiere autorización expresa de William. No cambiar ramas, crear ramas o worktrees, ni escribir en Jira/Confluence/GitHub en nombre del usuario si la tarea no lo autoriza.
- Al terminar, informar rama, archivos y cambios, validaciones permitidas, bloqueos y acciones pendientes del usuario. No hacer commits.

## Jira y Confluence

Para SOSpect, proyecto Jira `SDT`. Antes de crear una tarjeta, buscar equivalentes, relaciones y tickets cerrados recientemente; contrastar con el código/ramas cuando sea pertinente. Actualizar la tarea existente cuando corresponda y no duplicar.

Cuando un cambio no trivial de SOSpect deba registrarse: documentar primero en Confluence, crear/actualizar Jira después con el enlace a Confluence y luego completar el enlace inverso en Confluence. Toda escritura requiere aprobación explícita; las consultas de lectura no. Verificar estados, transiciones y taxonomía de labels actuales en Jira; no asumir IDs ni inventar etiquetas. No transicionar/cerrar tarjetas sin autorización específica.

## Documentación y planes

- Todo `.md` del proyecto se guarda bajo `D:\trabajo\wescotcorp\Docs\` en Windows o `/Users/wescot/GitHub/Docs/` en Mac, en la subcarpeta de capa apropiada. En Cloud usar la ruta Docs si está clonada; si no, no guardar una copia en una ruta inventada.
- Planes de SOSpect: `Docs/apps/sospect/plan/` (ruta equivalente del entorno). No crear planes en otra ubicación.
- El registro técnico de cambios no triviales va primero a Confluence según la sección anterior; no crear documentación redundante si el usuario no la necesita.

## Recursos de texto MAUI

Los textos de interfaz nuevos/modificados van en AppResources español. Reutilizar claves existentes; si se crea una nueva, `value` y `comment` deben ser el mismo texto español. Informar que quedan traducciones pendientes a los otros 74 idiomas para ResXManager.

Todo `Entry`, `Editor`, `Picker`, `TimePicker` y `DatePicker` nuevo/modificado requiere colores explícitos de fondo y texto, y colores de placeholder/título cuando correspondan. En `Label` sobre fondo claro, definir `TextColor` explícitamente por el modo oscuro iOS. No confiar solo en estilos implícitos ni en pruebas Android.

## Cambios propuestos pendientes de aprobación

El paquete de migración propone permitir commits preparados por Claude/Codex en ramas propias y una coordinación por ticket/PR. No cambia las reglas anteriores. No activar esa política, replicarla a `CLAUDE.md`, cambiar permisos, ni ejecutar sus pasos hasta aprobación expresa de William.
