﻿using System;
using System.Linq;
using System.Collections;
using PA_PrefabBuilder;
using System.Numerics;
using SVGToPrefab.Custom;

namespace SVGToPrefab
{
    /// <summary>
    /// This class is for applying data from our attributes and values to the GameObjectData class.
    /// Each function corresponds with the actual SVG attribute.
    /// </summary>
    class DataAppliers
    {
        public static void ApplyThisAttributeValue(ref GameObjectData obj, ref PathOutline pO, string value, string nextVal, float nextValf)
        {
            Shapes? shape = null;
            if (value == "d") {
                D(ref obj, nextVal, out shape);
                if (shape == null) Multi.PathOutline(ref pO, nextVal);
            }
            if (shape != null) obj.shape = shape.Value;
            switch (value) {
                case "x": case "cx": X(ref obj, nextValf); break;
                case "y": case "cy": Y(ref obj, nextValf); break;
                case "width": case "rx": sizeX(ref obj, nextValf, value); break;
                case "height": case "ry": sizeY(ref obj, nextValf, value); break;
                case "transform": translate(ref obj, nextVal); break;
                case "r": size(ref obj, nextValf); break;
                case "stroke": fill(ref obj, nextVal); stroke(ref obj); break;
                case "stroke-width": strokeWidth(ref pO, nextValf); break;
                case "fill": fill(ref obj, nextVal); break;
                case "x1": x1(ref pO, nextValf); break;
                case "x2": x2(ref pO, nextValf); break;
                case "y1": y1(ref pO, nextValf); break;
                case "y2": y2(ref pO, nextValf); break;
            }
        }
        public static void D(ref GameObjectData obj, string nextVal, out Shapes? shape)
        {
            shape = null;

            float[] xArray; // Array of X values 
            float[] yArray; // Array of Y values
            Vector2[] points; // Array of points
            CustomConversions.DPathToPoints(nextVal, out xArray, out yArray, out points);

            float SizeMultiplier = 1;

            // Analyze the number of points. We multiply by two because its split up by float, not by vector / float[2] then add two because of [M x y]
            switch (points.Length) {
                case 3:
                    shape = Shapes.Triangle;
                    SizeMultiplier = 2;
                break;
                case 4: shape = Shapes.Square; break;
                case 6: shape = Shapes.Hexagon; break;
            }
    
            // Get min and max
            float[] min = new float[] { xArray.Min(), yArray.Min() }; // float[2]
            float[] max = new float[] { xArray.Max(), yArray.Max() }; // float[2]

            // Get center and size
            float[] center = CustomMath.GetCenter(min, max);

            // Everything with final means its been rotated.
            Vector2[] finalPoints;
            float finalRotation;
            bool isRightTriangle;
            CustomMath.Rotations.GetRotatedShape(1f, CustomConversions.Float2ToVect(center), points, out finalPoints, out finalRotation, out isRightTriangle);

            if (isRightTriangle && points.Length == 3) obj.shapeVariant = 2;

            float[] finalXPoints = CustomConversions.VectIndexToFloatList(0, finalPoints); 
            float[] finalYPoints = CustomConversions.VectIndexToFloatList(1, finalPoints);

            float[] finalMin = new float[] { finalXPoints.Min(), finalYPoints.Min() }; // float[2]
            float[] finalMax = new float[] { finalXPoints.Max(), finalYPoints.Max() }; // float[2]

            float[] finalSize = CustomMath.GetSize(finalMin, finalMax); // float[2]
            // Questioning if I should use Vector or Point instead of float[2]

            LineWriter.WriteLine(center[0]);
            LineWriter.WriteLine(center[1]);
            LineWriter.WriteLine(finalSize[0]);
            LineWriter.WriteLine(finalSize[1]);

            // Apply center and size
            obj.positionX = center[0];
            obj.positionY = center[1];
            obj.sizeX = finalSize[0] * SizeMultiplier;
            obj.sizeY = finalSize[1] * SizeMultiplier;
            obj.rotAngle = finalRotation;
        }
        public static void X(ref GameObjectData obj, float nextValf)
        {
            obj.positionX = nextValf;
        }
        public static void Y(ref GameObjectData obj, float nextValf)
        {
            obj.positionY = nextValf;
        }
        public static void sizeX(ref GameObjectData obj, float nextValf, string value)
        {
            int multiplier = 1;
            if (value == "rx") multiplier = 2; // If this is an ellipse we want twice as much
            obj.sizeX = nextValf * multiplier;
        }
        public static void sizeY(ref GameObjectData obj, float nextValf, string value)
        {
            int multiplier = 1;
            if (value == "ry") multiplier = 2; // If this is an ellipse we want twice as much
            obj.sizeY = nextValf * multiplier;
        }
        public static void size(ref GameObjectData obj, float nextValf)
        {
            obj.sizeY = obj.sizeX = nextValf * 2;
        }
        public static void fill(ref GameObjectData obj, string nextVal)
        {
            int id;
            if (Program.IsItemIsInArray(nextVal, Input.Colors.ids, out id))
            {
                obj.colorNum = id; // Affirmative, colors count from 0.
            }
        }
        public static void stroke(ref GameObjectData obj)
        {
            obj.shapeVariant = 1;
        }
        public static void strokeWidth(ref PathOutline pO, float nextValf)
        {
            if (pO != null) pO.outlineSize = nextValf;
        }
        public static void rotation(ref GameObjectData obj, float nextValf)
        {
            obj.rotAngle = nextValf;
        }
        public static void translate(ref GameObjectData obj, string nextVal)
        {
            string[] vals = nextVal.Split('(', ')', ','); // Questioning if i should split for empty spaces..
            if (vals[0] == "matrix")
            {
                float[] matrix = new float[vals.Length - 1];

                for (int i = 1; i < vals.Length - 1; i++)
                {
                    matrix[i - 1] = float.Parse(vals[i]);
                }
                float posXdelta;
                float posYdelta;
                float sizeX; // Not valid at this point I think
                float sizeY; // Not valid at this point I think
                float rotation;
                CustomConversions.GetVarsFromMatrix(matrix, out posXdelta, out posYdelta, out sizeX, out sizeY, out rotation);

                // Consider making the offset center and push everything by 0.5?
                obj.offsetX = 0; 
                obj.offsetY = 0;
                //
                obj.positionX += 0.5f;
                obj.rotAngle = rotation;
                
            } else if (vals[0] == "rotate")
            {
                obj.rotAngle = float.Parse(vals[1]);
            }
        }
        public static void x1(ref PathOutline p0, float nextValf)
        {
            if (p0 == null) p0 = new PathOutline();
            p0.points[0].X = nextValf;
        }
        public static void x2(ref PathOutline p0, float nextValf)
        {
            if (p0 == null) p0 = new PathOutline();
            p0.points[1].X = nextValf;
        }
        public static void y1(ref PathOutline p0, float nextValf)
        {
            if (p0 == null) p0 = new PathOutline();
            p0.points[0].Y = nextValf;
        }
        public static void y2(ref PathOutline p0, float nextValf)
        {
            if (p0 == null) p0 = new PathOutline();
            p0.points[1].Y = nextValf;
        }
        struct Multi { 
            public static void PathOutline(ref PathOutline obj, string nextVal)
            {
                obj = new PathOutline();
                float[] xArray;
                float[] yArray;
                Vector2[] points;
                CustomConversions.DPathToPoints(nextVal, out xArray, out yArray, out points);
                obj.points = points;
            }
        
        }

    }
}
