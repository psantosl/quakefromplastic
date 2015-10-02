package com.codice.semanticmerge.examples.network;

import java.io.*;

class Socket {
    
    java.net.Socket socket;
    
    String getHostByName(String addr) {
        // this method returns a host
        // when you give an IP address
        return calculateHostByName(addr);
    }
    
    void listen() {
        // wait for connections on a port
        // and whatever is needed to listen
    }
    
    void connectTo(String addr, int port) throws Exception {
        // connect to a client
        socket = new java.net.Socket(addr, port);
    }
    
    int send(byte[] buffer) {
        try {
            OutputStream out = socket.getOutputStream();
            out.write(buffer);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
    
    int recv(byte[] buffer) {
        try {
            InputStream in = socket.getInputStream();
            in.read(buffer);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
    
}