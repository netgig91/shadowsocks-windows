using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

using NLog;

using Shadowsocks.Std.Controller;
using Shadowsocks.Std.Encryption;
using Shadowsocks.Std.Model;
using Shadowsocks.Std.Strategy;
using Shadowsocks.Std.Util;

namespace Shadowsocks.Std.Service
{
    internal class UDPRelay : Listener.Service
    {
        private readonly ShadowsocksController _controller;

        // TODO: choose a smart number
        private readonly LRUCache<IPEndPoint, UDPHandler> _cache = new LRUCache<IPEndPoint, UDPHandler>(512);

        public long outbound = 0;
        public long inbound = 0;

        public UDPRelay(ShadowsocksController controller)
        {
            this._controller = controller;
        }

        public override bool Handle(byte[] firstPacket, int length, Socket socket, object state)
        {
            if (socket.ProtocolType != ProtocolType.Udp)
            {
                return false;
            }
            if (length < 4)
            {
                return false;
            }
            Listener.UDPState udpState = (Listener.UDPState)state;
            IPEndPoint remoteEndPoint = (IPEndPoint)udpState.remoteEndPoint;
            UDPHandler handler = _cache.get(remoteEndPoint);
            if (handler == null)
            {
                handler = new UDPHandler(socket, _controller.GetAServer(IStrategyCallerType.UDP, remoteEndPoint, null/*TODO: fix this*/), remoteEndPoint);
                handler.Receive();
                _cache.add(remoteEndPoint, handler);
            }
            handler.Send(firstPacket, length);
            return true;
        }

        public class UDPHandler
        {
            private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

            private Socket _local;
            private Socket _remote;

            private Server _server;
            private byte[] _buffer = new byte[65536];

            private IPEndPoint _localEndPoint;
            private IPEndPoint _remoteEndPoint;

            private IPAddress GetIPAddress()
            {
                return _remote.AddressFamily switch
                {
                    AddressFamily.InterNetwork => IPAddress.Any,
                    AddressFamily.InterNetworkV6 => IPAddress.IPv6Any,
                    _ => IPAddress.Any,
                };
            }

            public UDPHandler(Socket local, Server server, IPEndPoint localEndPoint)
            {
                _local = local;
                _server = server;
                _localEndPoint = localEndPoint;

                // TODO async resolving
                bool parsed = IPAddress.TryParse(server.server, out IPAddress ipAddress);
                if (!parsed)
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(server.server);
                    ipAddress = ipHostInfo.AddressList[0];
                }
                _remoteEndPoint = new IPEndPoint(ipAddress, server.server_port);
                _remote = new Socket(_remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                _remote.Bind(new IPEndPoint(GetIPAddress(), 0));
            }

            public void Send(byte[] data, int length)
            {
                IEncryptorStrategy encryptor = EncryptorFactory.GetEncryptor(_server.method, _server.password);
                byte[] dataIn = new byte[length - 3];
                Array.Copy(data, 3, dataIn, 0, length - 3);
                byte[] dataOut = new byte[65536];  // enough space for AEAD ciphers
                int outlen;
                encryptor.EncryptUDP(dataIn, length - 3, dataOut, out outlen);
                _logger.Debug(_localEndPoint, _remoteEndPoint, outlen, "UDP Relay");
                _remote?.SendTo(dataOut, outlen, SocketFlags.None, _remoteEndPoint);
            }

            public void Receive()
            {
                EndPoint remoteEndPoint = new IPEndPoint(GetIPAddress(), 0);
                _logger.Debug($"++++++Receive Server Port, size:" + _buffer.Length);
                _remote?.BeginReceiveFrom(_buffer, 0, _buffer.Length, 0, ref remoteEndPoint, new AsyncCallback(RecvFromCallback), null);
            }

            public void RecvFromCallback(IAsyncResult ar)
            {
                try
                {
                    if (_remote == null) return;
                    EndPoint remoteEndPoint = new IPEndPoint(GetIPAddress(), 0);
                    int bytesRead = _remote.EndReceiveFrom(ar, ref remoteEndPoint);

                    byte[] dataOut = new byte[bytesRead];
                    int outlen;

                    IEncryptorStrategy encryptor = EncryptorFactory.GetEncryptor(_server.method, _server.password);
                    encryptor.DecryptUDP(_buffer, bytesRead, dataOut, out outlen);

                    byte[] sendBuf = new byte[outlen + 3];
                    Array.Copy(dataOut, 0, sendBuf, 3, outlen);

                    _logger.Debug(_localEndPoint, _remoteEndPoint, outlen, "UDP Relay");
                    _local?.SendTo(sendBuf, outlen + 3, 0, _localEndPoint);

                    Receive();
                }
                catch (ObjectDisposedException)
                {
                    // TODO: handle the ObjectDisposedException
                }
                catch (Exception)
                {
                    // TODO: need more think about handle other Exceptions, or should remove this catch().
                }
                finally
                {
                    // No matter success or failed, we keep receiving
                }
            }

            public void Close()
            {
                try
                {
                    _remote?.Close();
                }
                catch (ObjectDisposedException)
                {
                    // TODO: handle the ObjectDisposedException
                }
                catch (Exception)
                {
                    // TODO: need more think about handle other Exceptions, or should remove this catch().
                }
            }
        }
    }

    #region LRU cache

    // cc by-sa 3.0 http://stackoverflow.com/a/3719378/1124054
    internal class LRUCache<K, V> where V : UDPRelay.UDPHandler
    {
        private int capacity;
        private Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>> cacheMap = new Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>>();
        private LinkedList<LRUCacheItem<K, V>> lruList = new LinkedList<LRUCacheItem<K, V>>();

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public V get(K key)
        {
            LinkedListNode<LRUCacheItem<K, V>> node;
            if (cacheMap.TryGetValue(key, out node))
            {
                V value = node.Value.value;
                lruList.Remove(node);
                lruList.AddLast(node);
                return value;
            }
            return default(V);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void add(K key, V val)
        {
            if (cacheMap.Count >= capacity)
            {
                RemoveFirst();
            }

            LRUCacheItem<K, V> cacheItem = new LRUCacheItem<K, V>(key, val);
            LinkedListNode<LRUCacheItem<K, V>> node = new LinkedListNode<LRUCacheItem<K, V>>(cacheItem);
            lruList.AddLast(node);
            cacheMap.Add(key, node);
        }

        private void RemoveFirst()
        {
            // Remove from LRUPriority
            LinkedListNode<LRUCacheItem<K, V>> node = lruList.First;
            lruList.RemoveFirst();

            // Remove from cache
            cacheMap.Remove(node.Value.key);
            node.Value.value.Close();
        }
    }

    internal class LRUCacheItem<K, V>
    {
        public LRUCacheItem(K k, V v)
        {
            key = k;
            value = v;
        }

        public K key;
        public V value;
    }

    #endregion LRU cache
}