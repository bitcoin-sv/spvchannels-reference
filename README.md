# SPV Channels CE

Readme version 1.1.1.

| Contents | Version |
|-|-|
| SPV Channels Community Edition | 1.1.0 |

This repository contains SPV Channels CE, which is an implementation of the [BRFC specification](https://github.com/bitcoin-sv-specs/brfc-spvchannels) for SPV channels.
In addition to a server side implementation, it also contains the JavaScript client libraries for interacting with the server. See [Client libraries readme](client/javascript/readme.md) for more details about the client side libraries. 

SPV Channels provides a mechanism via which counterparties can communicate in a secure manner even in circumstances where one of the parties is temporarily offline.

## Swagger UI

The REST API can be reviewed in [Swagger UI](https://bitcoin-sv.github.io/spvchannels-reference/).

# Deploying SPV Channels CE API Server as docker containers on Linux

## Pre Requirements:
A SSL server certificate is required for installation. Obtain the certificate from your IT support team. There are are also services that issue free SSL certificates such as `letsencrypt.org`.  The certificate must be issued for the host with a fully qualified domain name. To use the server side certificate, you need to export it (including the corresponding private key) in PFX file format (*.pfx).

API Clients must trust the Certification Authority (CA) that issued the server side SSL certificate.

## Initial setup

The distribution is shared and run using Docker.

1. Open the terminal.

2. Create a directory where the spvchannels docker images, config and database will be stored (e.g. spvchannels) and navigate to it:

    ```
    mkdir spvchannels
    cd spvchannels
    ```    
   
3. Download the distribution of SPV Channels Server into the directory created in the previous step and extract the contents.

4. Check that the following files are present:

     - `docker-compose.yml`
     - `.env`
     
5. Create a `config` folder and copy the SSL server certificate file (<certificate_file_name>.pfx) into it. This server certificate is required to setup TLS (SSL).

6. Before running the SPV Channels API Server containers (spvchannels-db and spvchannels), replace some values in the `.env` file.

| Parameter | Description |
| --------- | ----------- |
|CERTIFICATEFILENAME|File name of the SSL server certificate (e.g. *<certificate_file_name.pfx>*) copied in step 5.|
|CERTIFICATESPASSWORD|Password of the *.pfx file copied in step 5.|
   > **Note:** The remaining setting are explaned in the section [Settings](#Settings).


## Running application
1. After the `.env` is set up, launch the spvchannels-db and spvchannels containers using the command:

    ```
    docker-compose up –d
    ```

The docker images as specified by the `docker-compose.yml` file, are automatically pulled from Docker Hub. 

2. Verify that all the SPV Channels Server containers are running using:

    ```
    docker ps
    ```
    The list should include `bitcoinsv/spvchannels-db` and `bitcoinsv/spvchannels`.
   
3. If everything is running you can continue to section [Account manager](#Account-manager:) to create an account.

> **Note:** If you were provided with an account id and its credentials then you can skip Setting up an account and proceed to [REST interface](#REST-interface)

## Setting up an account
To be able to call SPV Channels Server API, an account must be added into the database using the following command:

   ```
   docker exec spvchannels ./SPVChannels.API.Rest -createaccount [accountname] [username] [password]
   ```

Parameter description:

| Parameter | Description |
| ----------- | ----------- |
| [accountname] | name of the account, any whitespaces in accountname must be replaced with '_' |
| [username] | username of the account |
| [password] | password of the username |

   > **Note:** This command can also be used to add new users to an existing account (e.g. running `docker exec spvchannels ./SPVChannels.API.Rest -createaccount Accountname User1 OtherP@ssword` will return the account-id of Accountname).

## Setting up mobile push notifications
To enable mobile push notifications from SPV Channels, a Firebase service account key is required. Copy the *.json file containing the Firebase service account key into the config folder and set FIREBASECREDENTIALSFILENAME in the `.env` file.

>To get a Firebase service account *.json file, log in to your Firebase console and from Project Setting -> Service account -> Click on generate new private key. This will generate a *.json file with your Firebase service account key.


## REST interface

The reference implementation exposes different **REST APIs**:

* an API for managing channels
* an API for managing messages

This interfaces can be accessed on `https://<servername>:<port>/api/v1`. A Swagger page with the interface description can be accessed at `https://<servername>:<port>/swagger/index.html`
> **Note:** `<servername>` should be replaced with the name of the server where docker is running. `<port>` is set to 5010 by default in the `.env` file.

## Settings
| Parameter | Data type (allowed value) | Description |
| ----------- | ----------- | ----------- |
| NPGSQLLOGMANAGER | `<bool>` (`True|False`) | Enables additional database logging. Logs are in spvchannels-db container and can be accessed with the command (docker logs spvchannels-db). By default it's set to `False`. |
| HTTPSPORT | `<number>` | Port number on which SPV Channels API is running. By default it's set to `5010`. |
| CERTIFICATEFILENAME | `<text>` | File name of the SSL server certificate (e.g. *<certificate_file_name.pfx>*) |
| CERTIFICATESPASSWORD | `<text>` | Password of the *.pfx file |
| NOTIFICATIONTEXTNEWMESSAGE | `<text>` | Notification text upon arrival of a new message. By default it's set to `New message arrived`. |
| MAXMESSAGECONTENTLENGTH | `<number>` | Maximum size of any single message in bytes. By default it's set to its maximum size `65536`. |
| CHUNKEDBUFFERSIZE | `<number>` | If a message is sent in chunks, this sets the size of a chunk. By default it's set to `1024`. |
| TOKENSIZE | `<number>` | Length of bearer token. By default it's set to `64`. |
| CACHESIZE | `<number>` | Number of records in memorycache. By default it's set to `1048576`. |
| CACHESLIDINGEXPIRATIONTIME | `<number>` | Time in which a record is removed from memorycache if it is not accessed. By default it's set to `60` seconds. |
| CACHEABSOLUTEEXPIRATIONTIME | `<number>` | Time in which a record is removed from memorycache. By default it's set to `600` secunds. |
| FIREBASECREDENTIALSFILENAME | `<text>` | Fully qualified file name of the Firebase service account key. This setting is only required if you wish to enable mobile push notifications. See [Setting up mobile push notifications](#Setting-up-mobile-push-notifications)

## Terminating application

1. Open the terminal and navigate to spvchannels folder:

    ```
    cd spvchannels
    ```

2. To shutdown SPV Channels Server containers use the following command:

    ```
    docker-compose down
    ```
