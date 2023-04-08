using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ICities;
using UnityEngine;

namespace Nyoko.FlightLineCreator.Extensors
{
    class TLMAirplaneModifyRedirects : Redirector
    {
        private static TLMAirplaneModifyRedirects _instance;
        public static TLMAirplaneModifyRedirects instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TLMAirplaneModifyRedirects();
                }
                return _instance;
            }
        }

        public TLMAirplaneModifyRedirects()
        {
        }

        #region Hooks for PassengerShipAI

        public void OnCreated(ILoading loading)
        {
            doLog("TLMAirplaneModifyRedirects Criado!");
        }

        public Color GetColorBase(ushort vehicleID, ref Vehicle data, InfoManager.InfoMode infoMode)
        {
            return Color.black;
        }



        // PassengerAirplaneAI
        public Color GetColor(ushort vehicleID, ref Vehicle data, InfoManager.InfoMode infoMode)
        {
            if (infoMode == InfoManager.InfoMode.Transport)
            {
                ushort transportLine = data.m_transportLine;
                if (transportLine != 0)
                {
                    return Singleton<TransportManager>.instance.m_lines.m_buffer[(int)transportLine].GetColor();
                }
                return Singleton<TransportManager>.instance.m_properties.m_transportColors[(int)TransportInfo.TransportType.Airplane];
            }
            else
            {
                if (infoMode != InfoManager.InfoMode.Connections)
                {
                    ushort transportLine = data.m_transportLine;
                    if (transportLine != 0)
                    {
                        return Singleton<TransportManager>.instance.m_lines.m_buffer[(int)transportLine].GetColor();
                    }
                    return GetColorBase(vehicleID, ref data, infoMode);
                }
                InfoManager.SubInfoMode currentSubMode = Singleton<InfoManager>.instance.CurrentSubMode;
                if (currentSubMode == InfoManager.SubInfoMode.WindPower && (data.m_flags & (Vehicle.Flags.Importing | Vehicle.Flags.Exporting)) != Vehicle.Flags.Stopped)
                {
                    return Singleton<InfoManager>.instance.m_properties.m_modeProperties[(int)infoMode].m_targetColor;
                }
                return Singleton<InfoManager>.instance.m_properties.m_neutralColor;
            }
        }


        //info.m_vehicleAI.GetBufferStatus(firstVehicle, ref Singleton<VehicleManager>.instance.m_vehicles.m_buffer[(int)firstVehicle], out text, out fill, out cap);

        #endregion

        //#region Hooks for PublicTransportVehicleWorldInfoPanel
        //private void IconChanged(UIComponent comp, string text)
        //{

        //    PublicTransportVehicleWorldInfoPanel ptvwip = Singleton<PublicTransportVehicleWorldInfoPanel>.instance;
        //    ushort lineId = m_instance.TransportLine;
        //    UISprite iconSprite = ptvwip.gameObject.transform.Find("VehicleType").GetComponent<UISprite>();
        //    TLMUtils.doLog("lineId == {0}", lineId);
        //}
        //InstanceID m_instance;
        //#endregion

        public void OnReleased()
        {
        }

        #region Hooking
        private static Dictionary<MethodInfo, RedirectCallsState> redirects = new Dictionary<MethodInfo, RedirectCallsState>();

        public void EnableHooks()
        {
            if (redirects.Count != 0)
            {
                DisableHooks();
            }
            doLog("Loading Airplane Hooks!");
            AddRedirect(typeof(PassengerPlaneAI), typeof(TLMAirplaneModifyRedirects).GetMethod("GetColor", allFlags, null, new Type[] { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(InfoManager.InfoMode) }, null), ref redirects);
            AddRedirect(typeof(TLMAirplaneModifyRedirects), typeof(VehicleAI).GetMethod("GetColor", allFlags, null, new Type[] { typeof(ushort), typeof(Vehicle).MakeByRefType(), typeof(InfoManager.InfoMode) }, null), ref redirects, "GetColorBase");


        }

        public void DisableHooks()
        {
            foreach (var kvp in redirects)
            {
                RedirectionHelper.RevertRedirect(kvp.Key, kvp.Value);
            }
            redirects.Clear();
        }
        #endregion
    }
}
