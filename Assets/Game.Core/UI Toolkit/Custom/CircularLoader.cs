using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Core.UI_Toolkit.Custom
{
    /// <summary>
    /// A custom UI element that displays a circular loading indicator with a small segment
    /// that rotates around the circle. Progress loops between 0 and 100 in a continuous manner.
    /// The line width is automatically adjusted to stay within the rect boundaries.
    /// </summary>
    [UxmlElement]
    public partial class CircularLoader : VisualElement
    {
        // USS class names
        public static readonly string ussClassName = "circular-loader";
        public static readonly string ussLabelClassName = "circular-loader__label";

        // Custom style properties
        static CustomStyleProperty<Color> s_TrackColor = new CustomStyleProperty<Color>("--track-color");
        static CustomStyleProperty<Color> s_ProgressColor = new CustomStyleProperty<Color>("--progress-color");
        static CustomStyleProperty<Color> s_PercentageColor = new CustomStyleProperty<Color>("--percentage-color");
        static CustomStyleProperty<float> s_SegmentAngle = new CustomStyleProperty<float>("--segment-angle");
        
        // Default colors and properties
        Color m_TrackColor = new Color(0.51f, 0.51f, 0.51f); // Default track color (rgb(130, 130, 130))
        Color m_ProgressColor = new Color(0.18f, 0.52f, 0.09f); // Default progress color (rgb(46, 132, 24))
        Color m_PercentageColor = Color.white; // Default text color
        float m_LineWidth = 10f;
        float m_LineWidthPercentage = 0.2f; // Line width as percentage of radius
        float m_SegmentAngle = 45f; // Size of the progress segment in degrees

        // Label to display percentage
        Label m_Label;

        // Raw progress value that can go beyond 0-100 range
        float m_Progress;
        float m_MinProgress = 0;
        float m_MaxProgress = 100;

        [UxmlAttribute]
        public Color trackColor
        {
            get => m_TrackColor;
            set => m_TrackColor = value;
        }

        [UxmlAttribute]
        public Color progressColor
        {
            get => m_ProgressColor;
            set => m_ProgressColor = value;
        }

        /// <summary>
        /// Progress value that loops between 0 and 100
        /// </summary>
        [UxmlAttribute]
        public float progress
        {
            get => m_Progress;
            set
            {
                // Lưu lại giá trị gốc
                m_Progress = value;

                if (m_Progress > m_MaxProgress)
                {
                    value = m_MinProgress;
                    m_Progress = value;
                }
                else if (m_Progress < m_MinProgress)
                {
                    value = m_MaxProgress;
                    m_Progress = value;
                }
                

                m_Label.text = Mathf.Round(m_Progress) + "%";
                MarkDirtyRepaint();
            }
        }

        [UxmlAttribute]
        public float minProgress
        {
            get => m_MinProgress;
            set => m_MinProgress = value;
        }

        [UxmlAttribute]
        public float maxProgress
        {
            get => m_MaxProgress;
            set => m_MaxProgress = value;
        }


        /// <summary>
        /// Line width of the circle expressed as a percentage of the radius (0.0-1.0)
        /// </summary>
        [UxmlAttribute]
        public float lineWidthPercentage
        {
            get => m_LineWidthPercentage;
            set
            {
                // Clamp to ensure it's between 0 and 1 (0% to 100% of radius)
                m_LineWidthPercentage = Mathf.Clamp01(value);
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Size of the progress segment in degrees
        /// </summary>
        [UxmlAttribute]
        public float segmentAngle
        {
            get => m_SegmentAngle;
            set
            {
                m_SegmentAngle = Mathf.Clamp(value, 5f, 360f);
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Creates a new CircularLoader
        /// </summary>
        public CircularLoader()
        {
            // Create the label
            m_Label = new Label();
            m_Label.AddToClassList(ussLabelClassName);
            m_Label.style.flexGrow = new StyleFloat(1);
            m_Label.style.flexShrink = new StyleFloat(1);
            m_Label.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_Label.style.justifyContent = Justify.Center;
            m_Label.style.alignSelf = new StyleEnum<Align>(Align.Stretch);
            Add(m_Label);

            // Add UI class
            AddToClassList(ussClassName);

            // Register callbacks
            RegisterCallback<CustomStyleResolvedEvent>(CustomStylesResolved);
            generateVisualContent += GenerateVisualContent;

            progress = 0.0f;
        }

        static void CustomStylesResolved(CustomStyleResolvedEvent evt)
        {
            CircularLoader element = (CircularLoader)evt.currentTarget;
            element.UpdateCustomStyles();
        }

        void UpdateCustomStyles()
        {
            bool repaint = false;
            
            if (customStyle.TryGetValue(s_ProgressColor, out var progressColor))
            {
                m_ProgressColor = progressColor;
                repaint = true;
            }

            if (customStyle.TryGetValue(s_TrackColor, out var trackColor))
            {
                m_TrackColor = trackColor;
                repaint = true;
            }
            
            if (customStyle.TryGetValue(s_PercentageColor, out var percentageColor))
            {
                m_PercentageColor = percentageColor;
                m_Label.style.color = percentageColor;
                repaint = true;
            }
                
            if (customStyle.TryGetValue(s_SegmentAngle, out var segmentAngle))
            {
                m_SegmentAngle = segmentAngle;
                repaint = true;
            }

            if (repaint)
                MarkDirtyRepaint();
        }
        
        public float ConvertRange(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            // Đảm bảo giá trị đầu vào nằm trong phạm vi nguồn, nếu cần
            value = Mathf.Clamp(value, oldMin, oldMax);
    
            // Áp dụng công thức chuyển đổi tuyến tính
            return newMin + (value - oldMin) * (newMax - newMin) / (oldMax - oldMin);
        }

        void GenerateVisualContent(MeshGenerationContext context)
        {
            float width = contentRect.width;
            float height = contentRect.height;
            
            // Calculate the center and the maximum possible radius
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            float radius = Mathf.Min(width, height) * 0.5f;
            
            // Calculate the actual line width based on the percentage of radius
            m_LineWidth = (radius * ConvertRange(m_LineWidthPercentage, 0,1,0.1f, 0.9f));
            
            // Calculate the drawing radius (from center of line)
            float drawRadius = radius - m_LineWidth/2f;
    
            var painter = context.painter2D;
            painter.lineWidth = m_LineWidth;
            painter.lineCap = LineCap.Butt;

            // Draw the track (background circle)
            painter.strokeColor = m_TrackColor;
            painter.BeginPath();
            painter.Arc(center, drawRadius, 0.0f, 360.0f);
            painter.Stroke();


            // Draw the progress segment
            float segmentStart = -90f + ConvertRange(m_Progress, m_MinProgress, m_MaxProgress, 0, 100) * 3.6f;
            
            painter.strokeColor = m_ProgressColor;
            painter.BeginPath();
            painter.Arc(center,drawRadius, segmentStart, segmentStart + m_SegmentAngle);
            painter.Stroke();
        }
    }
}