using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JohnDeere.EIC.Models;
using JohnDeere.EIC.Models.RCD;
using JohnDeere.EIC.Models.RCD.Enums;
using JohnDeere.EIC.Models.RCD.Setup;
using JohnDeere.EIC.Models.RCD.Spatial;
using JohnDeere.EIC.ModelsImpl.Factories;
using JohnDeere.EIC.SpatialTools;
using JohnDeere.EIC.SpatialTools.Interfaces;
using JohnDeere.RepresentationSystem;
using JohnDeere.RepresentationReference;
using JohnDeere.UnitSystem;
using JohnDeere.EIC.Models.RCD.Log;
using System.IO;

namespace EICRead.Models
{
    public class AsApplied
    {
        protected static readonly Guid HostID =
            new Guid("{6FBE4481-EAD1-4890-9CBA-597DA13D4904}");

        public List<string> EquipmentConfigSections { set; get; }
        public List<string> EquipmentConfigNavigation { set; get; }
        public List<string> TaskMeasurementsMeters { set; get; }
        public List<string> LogOperationsMeters { set; get; }

        public AsApplied(string dirpath)
        {
            EquipmentConfigSections = new List<string>();
            EquipmentConfigNavigation = new List<string>();
            TaskMeasurementsMeters = new List<string>();
            LogOperationsMeters = new List<string>();
            
            string rcdpath = Path.Combine(dirpath, "RCD");
            GS2ModelFactory factory = GS2ModelFactory.Create(rcdpath, HostID);
            RcdCardModel model = factory.CreateCardModel();

            IList<IRcdStream> streamlist = model.GetStreams(TargetStreamType.Log);
            foreach (ILogStream strm in streamlist)
            {
                strm.Import();
                strm.ProcessSpatialLayers();

                foreach (ILogBlock logBlock in strm.LogBlocks)
                {
                    // Obtain task region
                    // Obtain spatial layer data. For each spatial point, obtain
                    // - long/lat
                    // - nav values
                    // - timestamp
                    // - meter rates (= meter data?)
                    // - section data?

                    // TaskRegion
                    ILogTaskRegion logTaskRegion = logBlock.ConfigBlock.TaskRegion;

                    // shapeLayers
                    int cShape = 0; // Temporary shape count
                    ILogTaskRegionSpatialShapeLayer shapeLayer = logTaskRegion.SpatialLayer;
                    shapeLayer.ResetShapeCursor();
                    while (shapeLayer.MoveToNextShape())
                    {
                        cShape++;

                        // shapeLayer info
                        string shapeTime = shapeLayer.Time.ToLongTimeString();
                        double latitude = shapeLayer.Latitude.Value.SourceValue;
                        double longitude = shapeLayer.Longitude.Value.SourceValue;
                        double elevation = shapeLayer.Elevation.Value.SourceValue;
                        string elevationrep = shapeLayer.Elevation.Value.Representation.ToString();
                        double distance = shapeLayer.DeltaDistance.Value.SourceValue;
                        string distancerep = shapeLayer.DeltaDistance.Representation.ToString();

                        // LogOperations
                        LogOperationsMeters.Add("LogOperations for shape " + cShape.ToString() + " at time " + shapeTime);
                        LogOperationsMeters.Add("--" + elevationrep + ": " + elevation + ", (" + latitude + "," + longitude + ")");
                        LogOperationsMeters.Add("--" + distancerep + ": " + distance);
                        int cLogOp = 0;
                        foreach (ILogOperation logOperation in logTaskRegion.LogOperations)
                        {
                            cLogOp++;
                            string product = "";
                            if (logOperation.Product != null)
                            {
                                product = logOperation.Product.Name;
                            }
                            else if (logOperation.ProductMixUse != null)
                            {
                                foreach (IProductMixComponent component in logOperation.ProductMixUse.Components)
                                {
                                    product += component.Product.Name + ":" + component.SolutionRate.ToString() + ", ";
                                }
                            }
                            else if (logOperation.Crop != null)
                            {
                                product += logOperation.Crop.Name;
                                if (logOperation.CropVariety != null)
                                {
                                    product += "\\" + logOperation.CropVariety.Name;
                                }
                            }
                            foreach (IMeter meter in logOperation.Meters)
                            {
                                foreach (ISection section in meter.Sections)
                                {
                                    DefinedTypeValue val = null;
                                    VariableNumber num = null;
                                    string valueStr = null;
                                    FetchMeterValues(section.RecordingStateElement, ref val, ref num, ref valueStr);
                                    LogOperationsMeters.Add("LogOp #" + cLogOp + ", product: " + product + ", " + formatLine(section.ID.ToString(), meter.ID.ToString(), section.RecordingStateElement.Representation.DomainID, valueStr));
                                    //foreach (IMeter meter2 in section.ReferringMeters)
                                    //{
                                    if (meter.TargetElement != null)
                                    {
                                        FetchMeterValues(meter.TargetElement, ref val, ref num, ref valueStr);
                                        LogOperationsMeters.Add("LogOp #" + cLogOp + ", product: " + product + ", " + formatLine(section.ID.ToString(), meter.ID.ToString(), meter.TargetElement.Representation.DomainID, valueStr));
                                    }
                                    if (meter.ControlElement != null)
                                    {
                                        FetchMeterValues(meter.ControlElement, ref val, ref num, ref valueStr);
                                        LogOperationsMeters.Add("LogOp #" + cLogOp + ", product: " + product + ", " + formatLine(section.ID.ToString(), meter.ID.ToString(), meter.ControlElement.Representation.DomainID, valueStr));
                                    }
                                    if (meter.MeasuredElement != null)
                                    {
                                        FetchMeterValues(meter.MeasuredElement, ref val, ref num, ref valueStr);
                                        LogOperationsMeters.Add("LogOp #" + cLogOp + ", product: " + product + ", " + formatLine(section.ID.ToString(), meter.ID.ToString(), meter.MeasuredElement.Representation.DomainID, valueStr));
                                    }
                                    if (meter.MeteredElement != null)
                                    {
                                        FetchMeterValues(meter.MeteredElement, ref val, ref num, ref valueStr);
                                        LogOperationsMeters.Add("LogOp #" + cLogOp + ", product: " + product + ", " + formatLine(section.ID.ToString(), meter.ID.ToString(), meter.MeteredElement.Representation.DomainID, valueStr));
                                    }
                                    //}
                                }
                            }
                        }
                        LogOperationsMeters.Add("---------------");

                        // Equipment Configurations
                        EquipmentConfigSections.Add("EquipmentConfig for shape " + cShape.ToString() + " at time " + shapeTime);
                        EquipmentConfigNavigation.Add("EquipmentConfig for shape " + cShape.ToString() + " at time " + shapeTime);
                        EquipmentConfigNavigation.Add("--" + elevationrep + ": " + elevation + ", (" + latitude + "," + longitude + ")");
                        EquipmentConfigNavigation.Add("--" + distancerep + ": " + distance);
                        foreach (IEquipmentConfiguration equipConfig in logBlock.ConfigBlock.EquipmentConfigurations)
                        {
                            foreach (ISection section in equipConfig.Sections)
                            {
                                DefinedTypeValue val = null;
                                VariableNumber num = null;
                                string valueStr = null;
                                FetchMeterValues(section.RecordingStateElement, ref val, ref num, ref valueStr);
                                EquipmentConfigSections.Add(formatLine(section.ID.ToString(), "", section.RecordingStateElement.Representation.DomainID, valueStr));
                                foreach (IMeter meter in section.ReferringMeters)
                                {
                                    if (meter.TargetElement != null)
                                    {
                                        FetchMeterValues(meter.TargetElement, ref val, ref num, ref valueStr);
                                        EquipmentConfigSections.Add(formatLine(section.ID.ToString(), meter.ID.ToString(), meter.TargetElement.Representation.DomainID, valueStr));
                                    }
                                    if (meter.ControlElement != null)
                                    {
                                        FetchMeterValues(meter.ControlElement, ref val, ref num, ref valueStr);
                                        EquipmentConfigSections.Add(formatLine(section.ID.ToString(), meter.ID.ToString(), meter.ControlElement.Representation.DomainID, valueStr));
                                    }
                                    if (meter.MeasuredElement != null)
                                    {
                                        FetchMeterValues(meter.MeasuredElement, ref val, ref num, ref valueStr);
                                        EquipmentConfigSections.Add(formatLine(section.ID.ToString(), meter.ID.ToString(), meter.MeasuredElement.Representation.DomainID, valueStr));
                                    }
                                    if (meter.MeteredElement != null)
                                    {
                                        FetchMeterValues(meter.MeteredElement, ref val, ref num, ref valueStr);
                                        EquipmentConfigSections.Add(formatLine(section.ID.ToString(), meter.ID.ToString(), meter.MeteredElement.Representation.DomainID, valueStr));
                                    }
                                }
                            }

                            int cNav = 0;
                            foreach (INavigationReference nav in equipConfig.NavigationReferences)
                            {
                                cNav++;
                                EquipmentConfigNavigation.Add("NAVIGATION " + cNav + ":");
                                if (nav.DeltaDistance != null)
                                {
                                    EquipmentConfigNavigation.Add(nav.DeltaDistanceRepresentation.DomainID + ": " + nav.DeltaDistance.Value.SourceValue + " " + nav.DeltaDistance.SourceUnitOfMeasure.ToString() + "/" + nav.DeltaDistance.TargetUnitOfMeasure.ToString());
                                }
                                if (nav.Elevation != null)
                                {
                                    EquipmentConfigNavigation.Add(nav.ElevationRepresentation.DomainID + ": " + nav.Elevation.Value.SourceValue + " " + nav.Elevation.SourceUnitOfMeasure.ToString() + "/" + nav.Elevation.TargetUnitOfMeasure.ToString());
                                }
                                if (nav.GPSAccuracy != null)
                                {
                                    EquipmentConfigNavigation.Add(nav.GPSAccuracyRepresentation.DomainID + ": " + nav.GPSAccuracy.Value.SourceValue + " " + nav.GPSAccuracy.SourceUnitOfMeasure.ToString() + "/" + nav.GPSAccuracy.TargetUnitOfMeasure.ToString());
                                }
                                if (nav.GPSVerticalAccuracy != null)
                                {
                                    EquipmentConfigNavigation.Add(nav.GPSVerticalAccuracyRepresentation.DomainID + ": " + nav.GPSVerticalAccuracy.Value.SourceValue + " " + nav.GPSVerticalAccuracy.SourceUnitOfMeasure.ToString() + "/" + nav.GPSVerticalAccuracy.TargetUnitOfMeasure.ToString());
                                }
                            }
                        }
                        EquipmentConfigSections.Add("---------------");
                        EquipmentConfigNavigation.Add("---------------");

                        // TaskMeasurements
                        TaskMeasurementsMeters.Add("TaskMeasurements for shape " + cShape.ToString() + " at time " + shapeTime);
                        TaskMeasurementsMeters.Add("--" + elevationrep + ": " + elevation + ", (" + latitude + "," + longitude + ")");
                        TaskMeasurementsMeters.Add("--" + distancerep + ": " + distance);
                        foreach (ITaskMeasurement taskMeasurement in logTaskRegion.TaskMeasurements)
                        {
                            DefinedTypeValue val = null;
                            VariableNumber num = null;
                            string valueStr = null;
                            //foreach (ISection section in taskMeasurement.Meter.Sections)
                            //{
                            //TaskMeasurementsMeters.Add(formatLine(/*section.ID.ToString()*/"N/A", taskMeasurement.Meter.ID.ToString(), section.RecordingStateElement.Representation.DomainID));
                            if (taskMeasurement.Meter.TargetElement != null)
                            {
                                FetchMeterValues(taskMeasurement.Meter.TargetElement, ref val, ref num, ref valueStr);
                                TaskMeasurementsMeters.Add(formatLine(/*section.ID.ToString()*/"Target", taskMeasurement.Meter.ID.ToString(), taskMeasurement.Meter.TargetElement.Representation.DomainID, valueStr));
                            }
                            if (taskMeasurement.Meter.ControlElement != null)
                            {
                                FetchMeterValues(taskMeasurement.Meter.ControlElement, ref val, ref num, ref valueStr);
                                TaskMeasurementsMeters.Add(formatLine(/*section.ID.ToString()*/"Control", taskMeasurement.Meter.ID.ToString(), taskMeasurement.Meter.ControlElement.Representation.DomainID, valueStr));
                            }
                            if (taskMeasurement.Meter.MeasuredElement != null)
                            {
                                FetchMeterValues(taskMeasurement.Meter.MeasuredElement, ref val, ref num, ref valueStr);
                                TaskMeasurementsMeters.Add(formatLine(/*section.ID.ToString()*/"Measured", taskMeasurement.Meter.ID.ToString(), taskMeasurement.Meter.MeasuredElement.Representation.DomainID, valueStr));
                            }
                            if (taskMeasurement.Meter.MeteredElement != null)
                            {
                                FetchMeterValues(taskMeasurement.Meter.MeteredElement, ref val, ref num, ref valueStr);
                                TaskMeasurementsMeters.Add(formatLine(/*section.ID.ToString()*/"Metered", taskMeasurement.Meter.ID.ToString(), taskMeasurement.Meter.MeteredElement.Representation.DomainID, valueStr));
                            }
                            //}
                        }
                        TaskMeasurementsMeters.Add("---------------");
                    }
                }
            }
        }

        private string formatLine(string sectionID, string meterID, string rep, string val = "")
        {
            return "S:" + sectionID + ", M:" + meterID + ", Rep:" + rep + ", Val:" + val /*+ "\n"*/;
        }

        private static ISpatialValue FetchMeterValues(
            IEquipmentElement elem,
            ref DefinedTypeValue val,
            ref VariableNumber num,
            ref string valueStr
            )
        {
            ISpatialValue value = null;

            if (elem != null)
            {
                if (elem.IsVRColumn)
                {
                    value = elem.VRSpatialColumn.Value;
                    num = ((ISpatialValueRepresentationNumber)value).Value;
                    if (num != null)
                    {
                        valueStr = num.ToString();
                    }
                    else
                    {
                        valueStr = "VR NULL VALUE";
                    }
                }
                else
                {
                    value = elem.DTSpatialColumn.Value;
                    val = ((ISpatialValueDefinedType)value).Value;
                    if (val != null)
                    {
                        valueStr = val.ToString();
                    }
                    else
                    {
                        valueStr = "DT NULL VALUE";
                    }
                }
            }

            if (num == null)
            {
                    num = new VariableNumber(RepresentationTagList.vrCropWeightVolume, new BaseNumber(
                477477, UnitSystemManager.Instance.UnitOfMeasures["lb1bu-1"]));
            }
            return value;
        }
    }
}