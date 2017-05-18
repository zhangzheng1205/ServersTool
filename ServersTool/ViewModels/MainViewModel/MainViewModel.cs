﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFrame.Net.TCP.Listener.Asynchronous;
using System.Windows;
using System.Net;
using FileOperate.Ini;
using System.IO;
using Caliburn.Micro;
using System.Diagnostics;
using System.Windows.Controls;
namespace ServersTool.ViewModels
{
    public class MainViewModel : MainViewModelFunc
    {
        public MainViewModel()
        {
            InitServer();
        }

        private string path = Environment.CurrentDirectory + "\\protocol.ini";
        private string section = "protocol";
        public void NewServer_Click(object sender, RoutedEventArgs e)
        {
            ServersInfo ser = new ServersInfo();
            ser.IPAddr = IPAddress.Parse("0.0.0.0");
            ser.Port = 8869;
            ser.IpEndPort = new IPEndPoint(ser.IPAddr, ser.Port);
            if (ServersList.Count != 0)
            {
                int temp = ServersList.ToList<ServersInfo>().FindIndex(ex => ex.IpEndPort.ToString() == ser.IpEndPort.ToString());
                if (temp != -1)
                {
                    MessageBox.Show("该服务器已添加");
                    return;
                }
            }

            ser.TcpServer = new AsyncTCPServer(ser.Port);
            ser.TcpServer.ClientConnected += TcpServer_ClientConnected;
            ser.TcpServer.ClientDisconnected += TcpServer_ClientDisconnected;
            ser.TcpServer.DataReceived += TcpServer_DataReceived;
            ServersList.Add(ser);
            SelectServer = ServersList[ServersList.Count - 1];
        }

        void TcpServer_DataReceived(object sender, AsyncEventArgs e)
        {
            TCPClientState client = e._state as TCPClientState;
            string remote = client.TcpClient.Client.RemoteEndPoint.ToString();

            string receStr = Encoding.Default.GetString(client.Buffer, 0, client.BufferLength);

            SelectServer.ReceiveText += receStr;

            string data = INIOperation.INIGetStringValue(path, section, receStr, null);
            if (data == null)
                return;
            byte[] aryData = Encoding.ASCII.GetBytes(data);
            SelectServer.TcpServer.Send(SelectServer.ClientsList[0].ClientState, aryData);
            SelectServer.SendText += data;
        }

        void TcpServer_ClientDisconnected(object sender, AsyncEventArgs e)
        {
            AsyncTCPServer server = sender as AsyncTCPServer;
            int temp = ServersList.ToList<ServersInfo>().FindIndex(ex => (ex.IPAddr.ToString() == server.Address.ToString() && ex.Port == server.Port));
            if (temp == -1)//列表中没找到服务器
                return;
            TCPClientState client = e._state as TCPClientState;
            EndPoint endpoint = client.TcpClient.Client.RemoteEndPoint;
            App.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                var tmp = ServersList[temp].ClientsList.First(ex => ex.IpEndPort == endpoint);
                ServersList[temp].ClientsList.Remove(tmp);
            }));
        }

        void TcpServer_ClientConnected(object sender, AsyncEventArgs e)
        {
            AsyncTCPServer server = sender as AsyncTCPServer;
            int temp = ServersList.ToList<ServersInfo>().FindIndex(ex => (ex.IPAddr.ToString() == server.Address.ToString() && ex.Port == server.Port));
            if (temp == -1)//列表中没找到服务器
                return;
            TCPClientState client = e._state as TCPClientState;
            ClientsInfo clientinfo = new ClientsInfo();
            clientinfo.IpEndPort = client.TcpClient.Client.RemoteEndPoint;
            clientinfo.ClientState = client;
            App.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                ServersList[temp].ClientsList.Add(clientinfo);
                IsHaveClent = true;
            }));
        }
        public void ServerStart_Click(object sender, RoutedEventArgs e)
        {
            if (ServersList.Count == 0 || SelectServer == null)
                return;
            SelectServer.TcpServer.Start();
            App.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                SelectServer.IsStart = true;

            }));
        }

        public void ServerClose_Click(object sender, RoutedEventArgs e)
        {
            if (ServersList.Count == 0 || SelectServer == null)
                return;
            SelectServer.TcpServer.Stop();
            ServersList.Remove(SelectServer);
        }


        public void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("作者：Lrsitod  联系邮箱：jhongbo@163.com", "Help");
        }
        /// <summary>
        /// 发送指令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Send_Click(object sender, RoutedEventArgs e)
        {
            if (SelectServer == null || SelectServer.ClientsList.Count == 0)
                return;
            if (string.IsNullOrEmpty(SelectServer.SendText))
                return;
            byte[] sendbytes = Encoding.Default.GetBytes(SelectServer.SendText);
            SelectServer.TcpServer.Send(SelectServer.ClientsList[0].ClientState, sendbytes);
            SelectServer.SendText = string.Empty;
        }

        /// <summary>
        /// 添加指令到指令库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Add_Click(object sender, RoutedEventArgs e)
        {
            Views.ProtocolView view = new Views.ProtocolView();
            ViewModelBinder.Bind(this, view, null);
            KeyText = SelectServer.ReceiveText;
            ValueText = SelectServer.SendText;
            if (view.ShowDialog() == true)
            {
                if (string.IsNullOrEmpty(KeyText) || string.IsNullOrEmpty(ValueText))
                    return;
                INIOperation.INIWriteValue(path, section, KeyText, ValueText);
            }
        }

        //打开指令文件
        public void OpenProtocol_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process proc = Process.Start(path);
            }
            catch
            {

            }
        }

        public void ClearReceiveText_Click(object sender, RoutedEventArgs e)
        {
            if (SelectServer != null)
            {
                SelectServer.ReceiveText = string.Empty;
            }
        }

        public void SelectedItemChanged(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = e.Source as TreeViewItem;
           
        }
    }
}
