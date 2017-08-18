# Medical-Augmented-Reality
iOS and Android Version of EC Medical Augmented Reality

# Sync branch ios <-> andorid
 
Imagine you are working on the android branch and you made some changes which should be also in ios.

- Check your latest commit in android and choose which you want to syncronize following these steps:

- Apply the changes introduced by the  last commit pointed to by android and create a new commit with these changes
git cherry-pick android~0 

- Apply the changes introduced by the  second last commit pointed to by android and create a new commit with these changes
git cherry-pick android~1 

 In the folder ec-europe-unity\Xcode-Project\Libraries\ there is a *.7z zip-file with an xcode library. As github only allows 100MB files with had to split it. Please use the 7Zip packing tool to unzip that library in the same folder.
http://www.7-zip.org/
libiPhone-lib.7z.001
libiPhone-lib.7z.002


#Parámetros para configurar la aplicación

   Archivo DatSetLoader.cs :
   
    // Número máximo de targets simultaneos que puede detectar en la app
    private int MAX_GAME_OBJECTS = 20;
    // Sensibilidad del Zoom
    private float zoomSensitivity = 0.0025f;  // 0.005f started with
    // Timeout en segundos cuando no encuentra un recurso.
    private float resourceCheckSeconds = 10;


#Parámetros del backend para conectar al server

   Archivo AppStartupController.cs :

    public const string APP_LANGUAGE = "ESP";
    public const string SERVER_DOMAIN = "http://ec-europe.inovum-solutions.com:8080";
    public const string SERVER_URI_GETAPP = "/get-app?identApp=" + APP_LANGUAGE;
    public const string SERVER_URI_GETAPPXML = "/get-app/xml?vuforiaAppDatabaseId=";
    public const string SERVER_URI_GETAPPDAT = "/get-app/dat?vuforiaAppDatabaseId=";
    public const string SERVER_URI_GETPRODUCTXML = "/get-product/xml?vuforiaProductDatabaseId=";
    public const string SERVER_URI_GETPRODUCTDAT = "/get-product/dat?vuforiaProductDatabaseId=";

#Versión de Unity a usar: Unity 5.3.4f1 (32-bit) ó superior

#Como crear un AssetBundle y exportarlo

Un AssetBundle es un archivo con extensión .unity3d que incluye uno o varios objectos 3d (fbx) con materiales y animaciones.
Este tipo de archivo es el que usa la aplicación (Android y IOS) para importar elementos de Unity a la misma.

Antes de realizar la exportación hay que tener en cuenta lo siguiente:

1) - El objecto u objectos 3d deben estar posicionados en la posición X=0,Y=0,z=0 de la escena.

2) - Debido a que la app utiliza una iluminación standard, es recomendado que los objectos usen materiales con shaders de texturas por defecto, para que la iluminación no afecte al objecto y sea lo más real posible. Se recomienda usar el shader: 

3) - La escala del objeto debe ser 1 para la correcta visualización en la app

## Exportación de un AssetBundle ##

Abrir el proyecto VuforiaAssetBundle con Unity3D.
En Build Settings cambiamos a la plataforma que queremos exportar Android o IOS.
Click en Switch Platform.
A continuación creamos un Prefab que incluya la escena ( objecto3d, materiales y animacion previamente creados o importados).
En el Project Assets View selecciona el Prefab a exportar. En la ventana de la parte derecha aparece visualizado el Prefab junto la opción AssetBundle.
Click en el dropdown y selecciona New AssetBundle e introduce el nombre del nuevo Asset.

Una vez defindo los parametros debemos ejecutar el script encargado de la exportación del AssetBundle, para ello vamos al menú Assets y hacemos click sobre la opción "Generate AssetBundles".

Según la plataforma que tengamos seleccionada habrá creado el archivo de salida en una u otra de las siguientes carpetas:

- GeneratedAndroidAssetBundles

- GeneratedIOSAssetBundles

El archivo generado tendrá el mismo nombre que introducimos en la configuración del AssetBundle.

El úlitmo paso es añadirle la extensión .unity3d al archivo.
Sin esta extensión Unity no trata al archivo como un AssetBundle

#Archivos de texto para traducción multidioma

Dentro de la carpeta Assets/Resources se encuentran las carpetas de los diferentes idiomas
Actualmente dentro de la carpeta "es" correspodiente al idioma castellano está incluido el archivo translations.json

Este archivo contiene los textos en idioma castellano.

#Conexión segura de los recursos con Amazon

Según el tipo de recurso la app utilizará una conexión segura o normal para descargar el recurso.

Cuando la URL del recurso comienza por ec-europe:// este es descargado usando las credenciales de Amazón y de forma segura.

Hay que tener en cuenta que este sistema  no funciona en el 100% de las conexiones WIFI, ya que muchos routers cortan la conexión a estos puertos, por lo que para que funcione al 100% la conexión debe ser directa de datos GPRS/3G/4G.

Cuando la url del recurso comienza por http este es descargado de forma común y directa.

#Configurar Amazon seguro en AWS y en la app

##Parametros en la app

 En el archivo DataSetLoader.cs:

// amazon private vars
    private string IdentityPoolId = "us-east-1:0aee2c38-fa8a-46e1-ba00-4004a2e41785";
    private string CognitoIdentityRegion = "us-east-1";
    public string S3Region = "eu-central-1";
    private string S3BucketName = "ec-europe";
    private string AWSResourceFile = null;
    private IAmazonS3 S3Client;
    private AWSCredentials Credentials;

Inicialización de Cognit Credentials de Amazon:

 // Amazon init
        UnityInitializer.AttachToGameObject(this.gameObject);
        Credentials = new CognitoAWSCredentials(IdentityPoolId, RegionEndpoint.GetBySystemName(CognitoIdentityRegion));
        S3Client = new AmazonS3Client(Credentials, RegionEndpoint.GetBySystemName(S3Region));

## Como configurar Amazon en AWS

1 - Configurar las politicas de seguridad del Bucket:

{
	"Version": "2012-10-17",
	"Id": "Policy1472727062830",
	"Statement": [
		{
			"Sid": "Stmt1472727060262",
			"Effect": "Allow",
			"Principal": "*",
			"Action": "s3:*",
			"Resource": "arn:aws:s3:::ec-europe"
		}
	]
}

##Recursos seguros

Para crear un recurso seguro subido en S3:

Hacer click botón derecho sobre el recurso :

En la ventana que aparece hacer click en botón "Add more permissions"

En el 2o input Grantee seleccionar : Any authenticated user

## How to compile and run Xcode (iOS) project
1. git checkout -b ios
2. git pull origin ios
3. Open 'Xcode-Project' directory
4. Launch 'Unity-iPhone.xcodeproj' project in xcode
5. Sign app with your developer certificate in the General of the Project settings.
6. Delete 'Android' folder from 'Libraries/Plugins' (otherwise you will get copypng error on build):
![01_Plugins-Android-delete.png](https://bitbucket.org/repo/4kG4Ax/images/500753842-01_Plugins-Android-delete.png)
7. Add two privacy keys to allow iOS access Camera and Photo Library in Info.plist (see next chapter)
![02_Add_two_keys.png](https://bitbucket.org/repo/4kG4Ax/images/2838832090-02_Add_two_keys.png)
8. Build & Run on your device
9. After build give your iPhone permission to run the app by allowing it in: Settings.app -> General -> Device Management. Click on your certificate and click "Trust" button.
10. Launch the "EC-Europe" app from the home screen.

## Generation of Xcode Project

When you build your project for iOS in Unity, it will ask for location where to safe the Xcode proejct and it's name (e.g. Xcode-EC-Europe).

After the project is saved you have to modify the *Info.plist* file.

Add two keys to it (either through Xcode visual editor or directly in the file):

	<key>NSCameraUsageDescription</key>
	<string>Vuforia</string>
	<key>NSPhotoLibraryUsageDescription</key>
	<string>Vuforia</string>
	
This will specify the security permissions for Camera and Photo Library for Vuforia framework. This has to be done __everytime__ the Xcode project is replaced (not appended) or created from the scract in Unity.

##Para debugar la aplicación ejecutando en el movil
- Se ha de activar el USB Debug en el móvil Android
- Se ejecuta en la consola el comando "adb logcat Unity:V *:S"

