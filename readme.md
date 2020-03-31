# SPV Channels CE

This repository contains SPV Channels Community Edition, which implements BRFC specification for SPV channels.
In addition do server side implementation,  it also contains JavaScript client libraries for interacting with the server. See [Client libraries readme](client/javascript/readme.md) for more details about client side libraries. 

SPV Channels provides a mechanism via which counterparties can communicate in a secure manner even in instances where one of the parties is temporarily offline.

# Deploying SPV Channels CE API Server as docker containers on Linux

## Pre Requirements:
A SSL server certificate is required for installation. You can obtain the certificate from your IT support team. There are are also services that issue free SSL certificates such as letsencrypt.org.  The certificate must be issued for the host with fully qualified domain name. To use the server side certificate, you need to export it (including corresponding private key) it in PFX file format (*.pfx).

API Clients must trust the Certification Authority (CA) that issued server side SSL certificate.

## Initial setup

For running in production environment, you should should use Docker.

1.	Open the terminal.

2. Create a directory where the spv docker images, config and database will be stored (e.g. spvchannels) and navigate to it.

    ```
    mkdir spvchannels
    cd spvchannels
    ```    
   
3. Download the distribution of SPV Channels Server into directory, created in the previous step and extract the content using the following command.
 
    ```
    unzip spvchannels.zip && rm spvchannels.zip
    ```

4.	Check that the following files are present:
     - `install.sh`
     - `uninstall.sh`
     - `docker-compose.yml`
     - `.env`
     
5. Next run the `install.sh` script. This script will load images `spvchannelsapi.tar` and `spvchannelsdata.tar` to docker and create a `config` folder, which will be required in the next step.
    
    ```
    bash install.sh
    ```

6.	Save SSL server certificate file  (*.pfx) into to the `config` folder. This server certificate is required to setup TLS (SSL).

7.	Before running the SPV Channels API Server containers (spvchannels-db and spvchannels-api), you must configure or replace some values in the `.env` file.

| Parameter | Description |
| --------- | ----------- |
|CERTIFICATE_FILENAME_VALUE|Fully qualified file name of the SSL server certificate (e.g. *<certificate_file_name.pfx>*) copied in step 5.|
|CERTIFICATES_PASSWORD_VALUE|The password of the *.pfx file copied in step 5.|
   > **Note:** The remaining setting are explaned in section [Settings](#Settings).

## Running application
1. After everything is set up and configured correctly, you can launch the spvchannels-db and spvchannels-api containers using the following command.

    ```
    docker-compose up –d
    ```

The docker images are automatically pulled from Docker Hub. 

2. Finally you can verify that all the SPV Channels Server containers are running (bitcoinsv/spvchannels-db and bitcoinsv/spvchannels).

    ```
    docker ps
    ```
   
10. If everything is running you can continue to section [Account manager](#Account-manager:) to create an account.

> **Note:** If you where provided whit an account id and its credentials then you can skip Setting up an account and proceed to [REST interface](#REST-interface)

## Setting up an account
To be able to call SPV Channels Server API, an account must be added into database using the following command.

    ```
    docker exec spvchannels-api ./SPVChannels.API.Rest -createaccount [accountname] [username] [password]
    ```

Parameter description:

| Parameter | Description |
| ----------- | ----------- |
| [accountname] | name of the account, if accoutname contains whitespaces, they shoul'd be replaced with '_' |
| [username] | username of the account |
| [password] | password of the username |

   > **Note:** This command can also be used to add new users to an existing account (e.g. running `docker exec spvchannels-api ./SPVChannels.API.Rest -createaccount Accountname User1 OtherP@ssword` will return account-id of Accountname).

## REST interface

The reference implementation exposes different **REST API** interfaces:

* an interface for managing channels
* an interface for managing messages

This interfaces can be accessed on `https://<servername>:<port>/api/v1`. Swagger page with interface description can be accessed at `https://<servername>:<port>/swagger/index.html`
> **Note:** `<servername>` should be replaced with the name of the server where docker is running. `<port>` is set to 5010 by default in the env file.

## Settings
| Parameter | Data type (allowed value) | Description |
| ----------- | ----------- | ----------- |
| NPGSQLLOGMANAGER | `<bool>` (`True|False`) | Enables additional database logging. Logs are in spvchannels-db container and can be accessed with the command (docker logs spvchannels-db). By defaulte it's set to `False`. |
| HTTPSPORT | `<number>` | Port number on witch SPV Channels API is running. By defaulte it's set to `5010`. |
| CERTIFICATEFILENAME | `<text>` | Fully qualified file name of the SSL server certificate (e.g. *<certificate_file_name.pfx>*) |
| CERTIFICATESPASSWORD | `<text>` | The password of the *.pfx file |
| NOTIFICATIONTEXTNEWMESSAGE | `<text>` | Notification text upon arrival of a new message. By defaulte it's set to `New message arrived`. |
| MAXMESSAGECONTENTLENGTH | `<number>` | The maximum size of any single message in bytes. By defaulte it's set to its maximum size `65536`. |
| CHUNKEDBUFFERSIZE | `<number>` | If a message is send in chunks, this sets the size of a chunk. By defaulte it's set to `1024`. |
| TOKENSIZE | `<number>` | Length of bearer token. By defaulte it's set to `64`. |
| CACHESIZE | `<number>` | Number of records in memorycach. By defaulte it's set to `1048576`. |
| CACHESLIDINGEXPIRATIONTIME | `<number>` | Time in witch a record is removed from memorycach if it is not accessed. By defaulte it's set to `60` secunds. |
| CACHEABSOLUTEEXPIRATIONTIME | `<number>` | Time in witch a record is removed from memorycach. By defaulte it's set to `600` secunds. |

## Terminating application

1. Open the terminal and navigate to spvchannels folder.

    ```
    cd spvchannels
    ```

2. To shutdown SPV Channels Server containers you run the following command.

    ```
    docker-compose down
    ```
   
   > **Note:** If you didn't execute step 8. in **Running application**, you can skip this step and proceed to the next step.

## Removing application

1. In this step you will remove the bitcoinsv/spvchannels-db:version and bitcoinsv/spvchannels::version images from docker with the script `uninstall.sh` and remove the `config` folder (including its content). 

    ```
    bash uninstall.sh [version]
    ```
   > **Note:** If you didn't execute step 5. in **Running application**, you can skip this step and proceed to the next step.    
   > **Note:** The `version` parameter is mandatory.

2. In the last step you will remove all the SPV Channels Server data by navigating one level up the folder hierarchy and removing the spvchannels folder (including its content) with the next commands (also you will be asked to enter the password). 

    ```
    cd ..
    sudo rm -rf spvchannels
    ```
   
   > **Note:** After executing step 4. all the SPV Channels Server data will be lost and can not be recovered!

