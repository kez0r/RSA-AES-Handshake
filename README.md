# **RSA/AES Handshake**

An example of an unauthenticated Client/Server public key exchange for further encrypted communication.   

## **_Methodology:_**  
1. **`[Client]`** generates a new RSA public/private key pair.  
2. **`[Client]`** connects to **`[Server]`** and transmits RSA public key.  
3. **`[Server]`** generates new AES key and initialization vector.  
4. **`[Server]`** encrypts AES key/iv with RSA public key from **`[Client]`** and transmits "handshake token".  
5. **`[Client]`** decrypts "handshake token" with RSA private key and sets session AES variables.
6. **`[Client]`** encrypts "challenge response" with session AES variables and transmits.
7. **`[Server]`** validates challenge response and responds with status (and restarts on failure).
8. **`[Client]`** verifies challenge response status.  
 
**`[Client]`** & **`[Server]`** now agree on session AES variables are can proceed to communicate on an encrypted channel.
