package com.codice.semanticmerge.examples.network;

import java.io.*;

class Socket {
    
    java.net.Socket socket;
    
    int send(byte[] buffer) {
        try {
            OutputStream outStream = socket.getOutputStream();
            outStream.write(buffer);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
    
    void listen() {
        // wait for connections on a port
        // and whatever is needed to listen
    }
    
    void connectTo(String addr, int port) throws Exception {
        // connect to a client
        socket = new java.net.Socket(addr, port);
    }

    class Dns {
    
        String getHostByName(String address) {
            // this method returns a host
            // when you give an IP address
            return calculateHostByName(address);
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