﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class Client
    {
        TcpClient client;
        Stream stream;
        public Client(TcpClient client, Stream stream)
        {
            this.client = client;
            this.stream = stream;
        }
        public Stream GetStream() {
            return this.stream;
        }
        public void Close()
        {
            this.stream.Close();
            this.client.Close();
        }
        public async Task<byte[]> Read(int n = 1)
        {
            byte[] buffer = new byte[n];
            long length = await this.stream.ReadAsync(buffer);
            byte[] data = new byte[length];
            Array.Copy(buffer, data, length);
            return data;
        }
        public async Task Write(byte[] data)
        {
            await this.stream.WriteAsync(data);
            await this.stream.FlushAsync();
        }
        public async Task zeroCopy(Stream stream)
        {
            await this.stream.CopyToAsync(stream);
            await this.stream.FlushAsync();
        }
    }
}