# Forja de Cuadros 1.0.2: .NET 10

Migración Windows x64 del 2026-10-23 de la aplicación mantenida independiente.
El snapshot histórico .NET 5 incluido en otro proyecto queda fuera del alcance.
SDK 10.0.402 fijado, C# 12 conservado y runtime Desktop 10.0.13 por aplicación.
No instalación global ni modificación de PATH, permisos o configuración del sistema.

## Preparación verificada

- Baseline original `9368700`: 19 pruebas, 86,50% líneas, 67,70% ramas y
  80,95% métodos del núcleo instrumentado, con umbrales 78/48/72.
- Primera etapa `f120d95`, versión interna 1.0.1: 68 pruebas y 100% en las tres
  métricas sobre .NET 8. Los umbrales suben a 100/100/100 sin nuevas exclusiones.
- La misma suite y el gate 100% pasan sobre .NET 10 con las dependencias de
  pruebas existentes. No se necesita una actualización masiva de paquetes.
- Autoprueba completa .NET 8: 5/5, video sintético, 16 cuadros no vacíos y únicos,
  atlas, PNG, revisión, GIF y SpriteFrames. Se conservan cuadros/hashes para
  comparar el paquete .NET 10 antes de adoptarlo.

## Corrección funcional y contratos

Las nuevas pruebas reprodujeron en .NET 8 un fallo previo de despill: un sujeto
opaco RGB (220,30,20) pasaba a (220,220,20) con clave verde; ocurría lo mismo
con claves roja/azul. Se limita el canal corregido a su valor original: el
despill reduce contaminación y no agrega color al sujeto. Tres pruebas fallaron
antes y pasaron después. Esta corrección intencional se distingue del cambio de
runtime; no se promete identidad de color con el fallo de la versión anterior.

Los fixtures cubren BGR/RGBA, alfa, erosión, islas, clipping, bordes, tres modos
de registro, orden/timestamps, JSON de opciones/metadatos, notificaciones,
previews, auditorías, firmas inválidas, intervalos mínimos y loops sintéticos.
Se retiraron cuatro comprobaciones inalcanzables por sus precondiciones:

- Dimensiones disponibles >=1 y bounds no vacíos garantizan escala positiva finita.
- Cutoff acotado y softness positiva garantizan upper > lower.
- Ambos llamadores de Median excluyen colecciones vacías.
- ReferenceDistance recibe >=3 firmas y agrega al menos la distancia 0/separation.

Las validaciones públicas de tamaños, canvas, suelo, raíz, null y firmas siguen
vigentes. La cobertura declarada corresponde al núcleo instrumentado; conserva
las exclusiones históricas de UI, FFmpeg y exportación, verificadas adicionalmente
mediante autoprueba, identidad, capturas y cierre. No equivale a cobertura total
de toda la aplicación.

## Publicación, QA y rollback

Publicar con el SDK fijado:

```powershell
dotnet publish src/ForjaDeCuadros/ForjaDeCuadros.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish/ForjaDeCuadros-1.0.2
```

Antes de adoptar el paquete: repetir autoprueba 5/5 y comparar sus 16 cuadros con
el baseline corregido .NET 8, probar identidad/splash descartable, capturas de
mesa/controles alfa y apertura/cierre del proceso propio. Usar únicamente medios
generados y `FORJA_DATA_DIRECTORY` temporal para sesiones/logs. Sin esa variable
se conserva la ubicación normal. `FORJA_KEEP_SELFTEST=1` retiene evidencia.
No abrir proveedores, cuentas, archivos reales ni integraciones externas.

Conservar ambas instalaciones/accesos anteriores, comprobar que no haya Forja
activa y copiar a carpetas versionadas nuevas antes de apuntar los accesos.
No cerrar procesos ajenos. El rollback restaura los accesos/binarios anteriores;
los datos reales permanecen intactos. Para código, revertir normalmente sin
reset ni force-push. Las copias privadas y evidencia se conservan al inventariar
temporales; no se borran automáticamente.

El ZIP SDK se verificó por SHA-512 oficial y firma Authenticode Microsoft.
El paquete debe registrar ambos frameworks 10.0.13 y su commit de código. Los
parches futuros requieren actualizar el pin, validar y republicar el paquete.

## Fuentes y límites

- [Descargas Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Metadatos oficiales](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json)
- [Soporte](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [Compatibilidad .NET 9](https://learn.microsoft.com/en-us/dotnet/core/compatibility/9.0)
- [Compatibilidad .NET 10](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10)

.NET 8.0.32 es el parche vigente de .NET 8 en esta fecha; su soporte termina el
2026-11-10. La antigüedad no demuestra una CVE. Esta entrega no certifica toda
la seguridad, calidad artística ni continuidad de proveedores externos. El cierre
de adopción y el estado de CI se registran con evidencia separada.
