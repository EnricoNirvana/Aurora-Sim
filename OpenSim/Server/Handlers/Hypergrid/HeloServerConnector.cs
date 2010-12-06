/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using Nini.Config;
using log4net;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Handlers.Base;
using OpenSim.Framework;

namespace OpenSim.Server.Handlers.Hypergrid
{
    public class HeloServiceInConnector : IServiceConnector
    {
        public string Name
        {
            get { return GetType().Name; }
        }

        public void Initialize(IConfigSource config, ISimulationBase simBase, string configName, IRegistryCore sim)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("HeloInHandler", "") != Name)
                return;

            IHttpServer server = simBase.GetHttpServer((uint)handlerConfig.GetInt("HeloInHandlerPort"));
            server.AddStreamHandler(new HeloServerGetHandler("opensim-robust"));
        }
    }

    public class HeloServerGetHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string m_HandlersType;

        public HeloServerGetHandler(string handlersType) :
            base("GET", "/helo")
        {
            m_HandlersType = handlersType;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            return OKResponse(httpResponse);
        }

        private byte[] OKResponse(OSHttpResponse httpResponse)
        {
            m_log.Debug("[HELO]: hi, I was called");
            httpResponse.AddHeader("X-Handlers-Provided", m_HandlersType);
            httpResponse.StatusCode = (int)HttpStatusCode.OK;
            httpResponse.StatusDescription = "OK";
            return new byte[0];
        }

    }
}
