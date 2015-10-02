package com.codice.semanticmerge.examples.network;

import java.io.*;

class ServerSocket {
    
    java.net.Socket socket;
    
    int recv(byte[] buffer) {
        try {
            InputStream in = socket.getInputStream();
            in.read(buffer);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
    
    void listen() {
        // wait for connections on a port
        // and whatever is needed to listen
    }
    
}