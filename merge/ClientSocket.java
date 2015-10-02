package com.codice.semanticmerge.examples.network;

import java.io.*;

class ClientSocket {
    
    java.net.Socket socket;
    
    int send(byte[] buffer) {
        try {
            OutputStream outStream = socket.getOutputStream();
            outStream.write(buffer);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
    
    void connectTo(String addr, int port) throws Exception {
        // connect to a client
        socket = new java.net.Socket(addr, port);
    }
}