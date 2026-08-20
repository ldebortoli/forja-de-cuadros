# Kaggle I2V en Forja de Cuadros

Kaggle ofrece notebooks con GPU compartida sin exigir una suscripción paga. Forja usa esa capacidad de forma opcional para ejecutar LTX-Video 2B y traer de vuelta un MP4 corto.

## Crear y verificar la cuenta

1. Abrí [Crear cuenta en Kaggle](https://www.kaggle.com/account/login?phase=startRegisterTab).
2. Registrate con Google o correo electrónico y elegí tu nombre de usuario. Ese nombre aparece en la URL del perfil y luego se escribe en Forja.
3. Confirmá el correo desde el mensaje que envía Kaggle.
4. Entrá en [Account settings](https://www.kaggle.com/settings/account) y completá la verificación telefónica. Kaggle exige una cuenta verificada para habilitar aceleradores GPU.
5. Abrí [Kaggle Notebooks](https://www.kaggle.com/notebooks) y revisá la cuota disponible. Si el selector de GPU todavía no aparece, la verificación no terminó o Kaggle está limitando temporalmente el acceso.

No hace falta crear manualmente un notebook ni descargar `kaggle.json`. Forja usa el OAuth actual de la CLI oficial. Como alternativa de diagnóstico, los tokens se administran en [API settings](https://www.kaggle.com/settings/api), pero nunca deben pegarse en un issue, commit o captura pública.

## Configurarlo dentro de Forja

1. Abrí `Forja de Cuadros` desde el acceso instalado.
2. En **01 Fuente**, pulsá `KAGGLE I2V`.
3. Seguí la guía visible del paso **01 Cuenta y GPU**. Cuando hayas creado la cuenta, confirmado el correo y completado la verificación requerida, marcá la casilla de confirmación. Si intentás conectar antes, Forja muestra un aviso y no abre OAuth.
4. Pulsá `PREPARAR KAGGLE`. La primera vez crea `%LOCALAPPDATA%\ForjaDeCuadros\Kaggle\cli` e instala allí la CLI oficial; requiere Python 3.11 o superior.
5. Pulsá `CONECTAR CUENTA`. Se abre Kaggle en el navegador. Iniciá sesión, autorizá el acceso y volvé a Forja.
6. Pulsá `VERIFICAR`. Esto confirma que la CLI puede consultar tu cuenta.
7. Escribí tu usuario de Kaggle, elegí una imagen PNG/JPG/WebP y ajustá el prompt, formato, duración, FPS y semilla.
8. Dejá activada la limpieza remota salvo que necesites conservar el trabajo para depurarlo.
9. Pulsá `SINCRONIZAR Y GENERAR`. Forja esperará aunque Kaggle ponga el trabajo en cola.
10. Cuando aparezca `MP4 LISTO`, pulsá `USAR ESTE VIDEO`; la ventana principal lo carga automáticamente.

## Qué sucede en Kaggle

```text
imagen + request.json
        ↓ dataset privado temporal
script privado + GPU NVIDIA T4
        ↓ LTX-Video 2B 0.9.8 distilled
forja-output.mp4
        ↓ descarga local
eliminación remota opcional
```

- El input y el script se crean privados; no se publican en el perfil.
- Forja solicita `NvidiaTeslaT4`. La documentación actual de Kaggle desaconseja el P100 con la imagen PyTorch predeterminada.
- El script usa una revisión fija del [repositorio oficial LTX-Video](https://github.com/Lightricks/LTX-Video) y descarga los pesos oficiales durante la ejecución. No se redistribuyen modelos dentro de Forja.
- El notebook necesita internet para clonar el código y descargar los pesos.
- Si cerrás o cancelás Forja después de enviar el trabajo, se detiene solamente la espera local; la GPU puede seguir trabajando. Usá `ABRIR TRABAJO` para verlo en Kaggle.

## Límites gratuitos

La GPU es gratuita pero compartida. Kaggle informa una cuota semanal —habitualmente alrededor de 30 horas, a veces mayor según demanda— que se reinicia semanalmente. Puede haber cola. Una ejecución de Notebook debe terminar dentro del máximo publicado por Kaggle, actualmente 12 horas para CPU/GPU, y `/kaggle/working` conserva hasta 20 GB de salida.

Revisá siempre la información vigente en [Notebooks](https://www.kaggle.com/docs/notebooks) y [uso eficiente de GPU](https://www.kaggle.com/docs/efficient-gpu-usage). Las cuotas y el hardware pueden cambiar sin que Forja publique una versión nueva.

## Privacidad y seguridad

- OAuth lo maneja la CLI oficial; Forja no recibe tu contraseña.
- Las credenciales quedan en la ubicación administrada por Kaggle CLI, fuera del repositorio y de las carpetas de trabajo.
- Cada job usa identificadores únicos; la limpieza solo apunta al dataset y kernel creados para ese job.
- Después de una descarga correcta, Forja elimina la copia temporal local de la imagen, el prompt y los archivos del job. Si el trabajo falla o se cancela, los conserva en `%LOCALAPPDATA%\ForjaDeCuadros\Kaggle\jobs` para diagnóstico.
- La imagen, el prompt y el MP4 se procesan en la infraestructura de Kaggle. No uses el modo cloud para contenido que no puedas enviar a ese servicio.
- Aplican los términos de Kaggle y la licencia del modelo LTX-Video. El usuario debe tener derechos sobre la imagen de entrada.

## Solución de problemas

- **No aparece GPU:** completá la verificación telefónica y revisá la cuota.
- **Trabajo en cola:** dejá Forja abierta; consulta el estado cada 20 segundos para respetar los límites dinámicos de la API.
- **Python no encontrado:** instalá Python 3.11+ desde [python.org](https://www.python.org/downloads/) y repetí `PREPARAR KAGGLE`.
- **OAuth no termina:** cerrá la pestaña fallida, repetí `CONECTAR CUENTA` y aceptá que el navegador abra el callback local.
- **El trabajo falla:** pulsá `ABRIR TRABAJO` y revisá el log. Conservá temporalmente el input desmarcando la limpieza solo cuando necesites depuración.
- **HTTP 429 / demasiadas solicitudes:** esperá unos minutos. Kaggle usa límites dinámicos y recomienda pausar antes de reintentar.

Referencias oficiales: [API y autenticación](https://www.kaggle.com/docs/api), [Kaggle CLI](https://github.com/Kaggle/kaggle-cli), [comandos de kernels](https://github.com/Kaggle/kaggle-cli/blob/main/docs/kernels.md) y [metadata de kernels](https://github.com/Kaggle/kaggle-cli/blob/main/docs/kernels_metadata.md).
