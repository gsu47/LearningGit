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
    public class Yield
    {
        protected static readonly Guid HostID =
            new Guid("{6FBE4481-EAD1-4890-9CBA-597DA13D4904}");

        public List<string> RelevantInfo { set; get; }
        //public List<string> EquipmentConfigSections { set; get; }
        //public List<string> EquipmentConfigNavigation { set; get; }
        //public List<string> TaskMeasurementsMeters { set; get; }
        //public List<string> LogOperationsMeters { set; get; }
        public double ApplicationArea;
        public double TotalYield;
        public int LogTypes;
        public string LogDate;
        public int YieldCount;
        public int MoistureCount;
        public int ElevationCount;
        public int SpeedCount;

        public enum LogType
        {
            Yield       = 1,
            AsApplied   = 2,
            AsPlanted   = 4
        }

        public Yield(string dirpath)
        {
            RelevantInfo = new List<string>();
            //EquipmentConfigSections = new List<string>();
            //EquipmentConfigNavigation = new List<string>();
            //TaskMeasurementsMeters = new List<string>();
            //LogOperationsMeters = new List<string>();

            YieldCount = 0;
            MoistureCount = 0;
            ElevationCount = 0;
            SpeedCount = 0;
            LogTypes = 0;

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

                    // Get Client/Farm/Field

                    // shapeLayers
                    int cShape = 0; // Temporary shape count
                    ILogTaskRegionSpatialShapeLayer shapeLayer = logTaskRegion.SpatialLayer;
                    shapeLayer.ResetShapeCursor();
                    while (shapeLayer.MoveToNextShape())
                    {
                        cShape++;

                        // shapeLayer info
                        LogDate = shapeLayer.Time.ToLongDateString();
                        string shapeTime = shapeLayer.Time.ToString();
                        double latitude = shapeLayer.Latitude.Value.SourceValue;
                        double longitude = shapeLayer.Longitude.Value.SourceValue;
                        string elevationrep     = shapeLayer.Elevation.Value.Representation.ToString();
                        double elevationSV      = shapeLayer.Elevation.Value.SourceValue;
                        string elevationSVuom   = shapeLayer.Elevation.Value.SourceUnitOfMeasure.ToString();
                        shapeLayer.Elevation.Value.TargetUnitOfMeasure = UnitSystemManager.Instance.UnitOfMeasures["ft"];
                        double elevationTV      = shapeLayer.Elevation.Value.TargetValue;
                        string elevationTVuom   = shapeLayer.Elevation.Value.TargetUnitOfMeasure.ToString();
                        string distancerep      = shapeLayer.DeltaDistance.Value.Representation.ToString();
                        double distanceSV       = shapeLayer.DeltaDistance.Value.SourceValue;
                        string distanceSVuom    = shapeLayer.DeltaDistance.Value.SourceUnitOfMeasure.ToString();
                        shapeLayer.DeltaDistance.Value.TargetUnitOfMeasure = UnitSystemManager.Instance.UnitOfMeasures["ft"];
                        double distanceTV       = shapeLayer.DeltaDistance.Value.TargetValue;
                        string distanceTVuom    = shapeLayer.DeltaDistance.Value.TargetUnitOfMeasure.ToString();

                        RelevantInfo.Add("Shape " + cShape.ToString() + " at time " + shapeTime);
                        RelevantInfo.Add("(" + latitude + "," + longitude + ")");
                        RelevantInfo.Add("--" + distancerep + ": " + distanceSV + " " + distanceSVuom + ", " + distanceTV + " " + distanceTVuom);
                        int cLogOp = 0;
                        foreach (ILogOperation logOperation in logTaskRegion.LogOperations)
                        {
                            cLogOp++;

                            // Get the product. This could also indicate the type of log file: yield/asapplied/asplanted?
                            string product = "";
                            if (logOperation.Product != null)
                            {
                                product = logOperation.Product.Name;
                                //LogType = "AsApplied";
                            }
                            else if (logOperation.ProductMixUse != null)
                            {
                                foreach (IProductMixComponent component in logOperation.ProductMixUse.Components)
                                {
                                    product += component.Product.Name + ":" + component.SolutionRate.ToString() + ", ";
                                }
                                //LogType = "AsApplied";
                            }
                            else if (logOperation.Crop != null)
                            {
                                product += logOperation.Crop.Name;
                                if (logOperation.CropVariety != null)
                                {
                                    product += "\\" + logOperation.CropVariety.Name;
                                }
                                //LogType = "Yield? AsPlanted?";
                            }

                            foreach (IMeter meter in logOperation.Meters)
                            {
                                DefinedTypeValue val = null;
                                VariableNumber num = null;
                                string valueStr = null;

                                //foreach (ISection section in meter.Sections)
                                //{
                                    //FetchMeterValues(section.RecordingStateElement, ref val, ref num, ref valueStr);
                                    //if (valueStr == "dtiRecordingStatusOn")
                                    //{
                                    //    RelevantInfo.Add("LogOp #" + cLogOp + ", product: " + product + ", " + formatLine(section.ID.ToString(), meter.ID.ToString(), section.RecordingStateElement.Representation.DomainID, valueStr));
                                        //if (meter.TargetElement != null)
                                        //{
                                        //    FetchMeterValues(meter.TargetElement, ref val, ref num, ref valueStr);
                                        //    RelevantInfo.Add("LogOp #" + cLogOp + ", product: " + product + ", " + formatLine(section.ID.ToString(), meter.ID.ToString(), meter.TargetElement.Representation.DomainID, valueStr));
                                        //}
                                        //if (meter.ControlElement != null)
                                        //{
                                        //    FetchMeterValues(meter.ControlElement, ref val, ref num, ref valueStr);
                                        //    RelevantInfo.Add("LogOp #" + cLogOp + ", product: " + product + ", " + formatLine(section.ID.ToString(), meter.ID.ToString(), meter.ControlElement.Representation.DomainID, valueStr));
                                        //}
                                        if (meter.MeasuredElement != null)
                                        {
                                            //FetchMeterValues(meter.MeasuredElement, ref val, ref num, ref valueStr);
                                            //RelevantInfo.Add("LogOp #" + cLogOp + ", product: " + product + ", " + formatLine(section.ID.ToString(), meter.ID.ToString(), meter.MeasuredElement.Representation.DomainID, valueStr));
                                            FetchMeterValues(meter.MeasuredElement, ref val, ref num, ref valueStr);
                                            if (num != null)
                                            {
                                                if (meter.MeasuredElement.Representation.DomainID == "vrYieldWetMass")
                                                {
                                                    //RelevantInfo.Add("YieldPoint: " + valueStr);
                                                    LogTypes |= LogType.Yield.GetHashCode();
                                                    YieldCount++;
                                                    num.TargetUnitOfMeasure = UnitSystemManager.Instance.UnitOfMeasures["lb"];
                                                    RelevantInfo.Add("YieldPoint: " + num.SourceValue + " " + num.SourceUnitOfMeasure + ", " + num.TargetValue + " " + num.TargetUnitOfMeasure);
                                                    TotalYield += num.TargetValue; // These are in grams not kg?
                                                }
                                                else if (meter.MeasuredElement.Representation.DomainID == "vrHarvestMoisture")
                                                {
                                                    MoistureCount++;
                                                    //RelevantInfo.Add("MoisturePoint: " + valueStr);
                                                    RelevantInfo.Add("MoisturePoint: " + num.SourceValue + " " + num.SourceUnitOfMeasure + ", " + num.TargetValue + " " + num.TargetUnitOfMeasure);
                                                }
                                                else if (meter.MeasuredElement.Representation.DomainID == "vrSeedRateSeedsMeasured" ||
                                                         meter.MeasuredElement.Representation.DomainID == "vrSeedRateMassMeasured")
                                                {
                                                    LogTypes |= LogType.AsPlanted.GetHashCode();
                                                }
                                                else if (meter.MeasuredElement.Representation.DomainID == "vrAppRateMassMeasured" ||
                                                         meter.MeasuredElement.Representation.DomainID == "vrAppRateVolumeMeasured")
                                                {
                                                    LogTypes |= LogType.AsApplied.GetHashCode();
                                                }
                                            }
                                        }
                                        //if (meter.MeteredElement != null)
                                        //{
                                        //    FetchMeterValues(meter.MeteredElement, ref val, ref num, ref valueStr);
                                        //    RelevantInfo.Add("LogOp #" + cLogOp + ", product: " + product + ", " + formatLine(section.ID.ToString(), meter.ID.ToString(), meter.MeteredElement.Representation.DomainID, valueStr));
                                        //}
                                    //}
                                //}
                            }
                        }

                        VariableNumber SectionArea = new VariableNumber(RepresentationTagList.vrTaskArea, new BaseNumber(0, UnitSystemManager.Instance.UnitOfMeasures["ac"]));
                        VariableRepresentation rep = RepresentationManager.Instance.Representations[RepresentationTagList.vrTaskArea] as VariableRepresentation;
                        double SectionWidth = 0;
                        foreach (IEquipmentConfiguration equipConfig in logBlock.ConfigBlock.EquipmentConfigurations)
                        {
                            // Find active sections
                            if (equipConfig.Sections != null)
                            {
                                foreach (ISection section in equipConfig.Sections)
                                {
                                    DefinedTypeValue val = null;
                                    VariableNumber num = null;
                                    string valueStr = null;
                                    FetchMeterValues(section.RecordingStateElement, ref val, ref num, ref valueStr);
                                    if (valueStr == "dtiRecordingStatusOn")
                                    {
                                        section.Width.TargetUnitOfMeasure = UnitSystemManager.Instance.UnitOfMeasures["ft"];
                                        SectionWidth += section.Width.TargetValue;
                                        SectionArea.AddInPlace(section.Width.Multiply(shapeLayer.DeltaDistance.Value, rep));
                                    }
                                }
                                break;
                            }
                        }
                        // Calculate area
                        SectionArea.TargetUnitOfMeasure = UnitSystemManager.Instance.UnitOfMeasures["ac"];
                        RelevantInfo.Add("Area: " + SectionWidth + " * " + distanceTV + " = " + SectionArea.TargetValue);
                        //ApplicationArea += SectionWidth * distanceTV;
                        ApplicationArea += SectionArea.TargetValue;
                        
//#define OLD
#if OLD
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
#endif

                        // TaskMeasurements
                        //TaskMeasurementsMeters.Add("TaskMeasurements for shape " + cShape.ToString() + " at time " + shapeTime);
                        //TaskMeasurementsMeters.Add("--" + elevationrep + ": " + elevation + ", (" + latitude + "," + longitude + ")");
                        //TaskMeasurementsMeters.Add("--" + distancerep + ": " + distance);
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
                                //FetchMeterValues(taskMeasurement.Meter.TargetElement, ref val, ref num, ref valueStr);
                                //TaskMeasurementsMeters.Add(formatLine(/*section.ID.ToString()*/"Target", taskMeasurement.Meter.ID.ToString(), taskMeasurement.Meter.TargetElement.Representation.DomainID, valueStr));
                                if (taskMeasurement.Meter.TargetElement.Representation.DomainID == "vrVehicleSpeed")
                                {
                                    FetchMeterValues(taskMeasurement.Meter.TargetElement, ref val, ref num, ref valueStr);
                                    if (num != null)
                                    {
                                        SpeedCount++;
                                        num.TargetUnitOfMeasure = UnitSystemManager.Instance.UnitOfMeasures["mi1hr-1"];
                                        RelevantInfo.Add("SpeedPoint: " + num.SourceValue + " " + num.SourceUnitOfMeasure + ", " + num.TargetValue + " " + num.TargetUnitOfMeasure);
                                    }
                                }
                            }
                            //if (taskMeasurement.Meter.ControlElement != null)
                            //{
                            //    FetchMeterValues(taskMeasurement.Meter.ControlElement, ref val, ref num, ref valueStr);
                            //    //TaskMeasurementsMeters.Add(formatLine(/*section.ID.ToString()*/"Control", taskMeasurement.Meter.ID.ToString(), taskMeasurement.Meter.ControlElement.Representation.DomainID, valueStr));
                            //}
                            //if (taskMeasurement.Meter.MeasuredElement != null)
                            //{
                            //    FetchMeterValues(taskMeasurement.Meter.MeasuredElement, ref val, ref num, ref valueStr);
                            //    //TaskMeasurementsMeters.Add(formatLine(/*section.ID.ToString()*/"Measured", taskMeasurement.Meter.ID.ToString(), taskMeasurement.Meter.MeasuredElement.Representation.DomainID, valueStr));
                            //}
                            //if (taskMeasurement.Meter.MeteredElement != null)
                            //{
                            //    FetchMeterValues(taskMeasurement.Meter.MeteredElement, ref val, ref num, ref valueStr);
                            //    //TaskMeasurementsMeters.Add(formatLine(/*section.ID.ToString()*/"Metered", taskMeasurement.Meter.ID.ToString(), taskMeasurement.Meter.MeteredElement.Representation.DomainID, valueStr));
                            //}
                            //}
                        }
                        //TaskMeasurementsMeters.Add("---------------");

                        ElevationCount++;
                        RelevantInfo.Add("ElevationPoint: " + elevationSV + " " + elevationSVuom + ", " + elevationTV + " " + elevationTVuom);
                        RelevantInfo.Add("---------------");
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

            return value;
        }
    }
}
