# Proveedores externos de imagen a video

Revisión: 22 de agosto de 2026. Las cuotas y modelos cambian seguido; confirmá siempre el saldo que muestra cada sitio antes de generar.

Forja no integra cuentas, APIs ni cobros. El paso 01 solamente conserva el PNG croma preparado y abre los proveedores que hayan sido aprobados. El MP4 se descarga y se carga manualmente en el paso 02.

## Aprobado

### Grok Imagine

- Estado: aprobado por el usuario después de una prueba real.
- Sirve para imagen a video y conserva el aspecto de la imagen mejor que el primer flujo local evaluado.
- La documentación de xAI admite imagen a video, clips de 1 a 15 segundos y resoluciones de 480p, 720p y 1080p. La cuota gratuita de la aplicación no está publicada como una cantidad estable: hay que mirar el límite de la cuenta.
- Enlace: <https://grok.com/imagine>
- Referencia oficial: <https://docs.x.ai/developers/model-capabilities/video/generation>

## Candidatos para probar antes de agregarlos a la aplicación

### Google Flow / Veo 3.1

- Prioridad de prueba: alta.
- La portada y la ayuda de créditos publican 50 créditos diarios sin suscripción. Veo 3.1 Lite cuesta 10 créditos por generación, por lo que el máximo teórico es cinco generaciones diarias si cada solicitud produce un solo video.
- Incluye Frames to Video, Ingredients to Video y Characters, funciones valiosas para mantener identidad y reutilizar equipo.
- Argentina figura entre las regiones admitidas.
- Advertencia: otra página de alta todavía afirma que hace falta una suscripción. Puede depender del despliegue o de la cuenta; conviene probar el acceso gratuito antes de aprobarlo. Los planes gratuitos muestran marca de agua visible.
- Enlaces oficiales: <https://labs.google/fx/tools/flow>, <https://support.google.com/flow/answer/16526234?hl=es-419> y <https://support.google.com/flow/answer/16353544?hl=es-419>.

### Adobe Firefly

- Prioridad de prueba: alta.
- Adobe anuncia generaciones diarias gratuitas con una cuenta sin cargo; la cantidad puede variar.
- Permite imagen inicial y, opcionalmente, imagen final, control de cámara y descarga MP4. El modelo propio de Firefly se presenta como apto para uso comercial; esta afirmación no se extiende automáticamente a sus modelos asociados.
- Enlace oficial: <https://www.adobe.com/products/firefly/features/image-to-video.html>

### Pika

- Prioridad de prueba: media.
- El plan Basic cuesta 0, renueva 80 créditos mensuales y limita Pika 2.5 a 480p.
- Un video I2V de 5 segundos cuesta 12 créditos: entran seis generaciones completas por mes y sobran ocho créditos.
- La tabla actual indica descarga sin marca de agua y uso comercial incluso en Basic.
- Enlace oficial: <https://pika.art/pricing?interval=month>

### PixVerse

- Prioridad de prueba: media.
- La documentación de la aplicación confirma créditos diarios que se renuevan a las 00:00 UTC; no publica una cantidad gratuita estable.
- Cada generación consume 20, 40 o 60 créditos según el modo. Si el video falla, informa que no descuenta el costo.
- Enlace oficial: <https://app.pixverse.ai/>
- Referencia oficial: <https://docs.pixverse.ai/FAQ-Subscriptions-Billing-45723f6092f740bca8f0209878440ca1>

## No recomendados para este flujo gratuito recurrente

### Runway

La cuenta gratuita entrega 125 créditos una sola vez, no se renuevan y los videos llevan marca de agua. Es útil como comparación puntual de calidad, no como fuente recurrente. Referencia: <https://help.runwayml.com/hc/en-us/articles/50404627334547-Free-plan-details>.

### Luma Dream Machine

El plan gratuito tiene resolución de borrador, marca de agua y licencia sólo personal/no comercial. No conviene usar sus resultados en un juego que se publicará. Referencias: <https://lumalabs.ai/learning-hub/payments-subscriptions> y <https://lumalabs.ai/learning-hub/licensing>.

## Prueba corta para decidir

Usar exactamente la misma imagen croma y el mismo prompt en cada candidato:

> Full-body 2D inked game character performs one clean animation cycle. Fixed camera, no zoom, stable identity and clothing, rigid equipment, feet remain visible, uniform bright green background, no shadow, smooth readable motion, final pose matches the first pose.

Comparar identidad de máscara y pelo, manos y pies, rigidez de arma/escudo, limpieza del fondo, continuidad del movimiento y posibilidad legal de usar el MP4 en el juego. Un proveedor se agrega a Forja sólo después de que el usuario apruebe una prueba real.
