using UnityEngine;
using UnityEngine.UI;

namespace SABI
{
    public static class GraphicExtensions
    {
        /// Extension method for Graphic that sets the red color channel.
        /// Return this Graphic for method chaining.
        /// Arguments: float colorR: Red channel value.
        public static Graphic SetColorR(this Graphic graphic, float colorR)
        {
            graphic.color = graphic.color.WithR(colorR);
            return graphic;
        }

        /// Extension method for Graphic that sets the green color channel.
        /// Return this Graphic for method chaining.
        /// Arguments: float colorG: Green channel value.
        public static Graphic SetColorG(this Graphic graphic, float colorG)
        {
            graphic.color = graphic.color.WithG(colorG);
            return graphic;
        }

        /// Extension method for Graphic that sets the blue color channel.
        /// Return this Graphic for method chaining.
        /// Arguments: float colorB: Blue channel value.
        public static Graphic SetColorB(this Graphic graphic, float colorB)
        {
            graphic.color = graphic.color.WithB(colorB);
            return graphic;
        }

        /// Extension method for Graphic that sets the alpha channel.
        /// Return this Graphic for method chaining.
        /// Arguments: float colorA: Alpha channel value.
        public static Graphic SetColorA(this Graphic graphic, float colorA)
        {
            graphic.color = graphic.color.WithA(colorA);
            return graphic;
        }

        /// Extension method for Graphic that sets the RGB color channels.
        /// Return this Graphic for method chaining.
        /// Arguments: float colorR: Red, float colorG: Green, float colorB: Blue channel values.
        public static Graphic SetColorRGB(
            this Graphic graphic,
            float colorR,
            float colorG,
            float colorB
        )
        {
            graphic.color = graphic.color.WithRGB(colorR, colorG, colorB);
            return graphic;
        }

        /// Extension method for Graphic that sets the RGBA color channels.
        /// Return this Graphic for method chaining.
        /// Arguments: float colorR: Red, float colorG: Green, float colorB: Blue, float colorA: Alpha channel values.
        public static Graphic SetColorRGB(
            this Graphic graphic,
            float colorR,
            float colorG,
            float colorB,
            float colorA
        )
        {
            graphic.color = graphic.color.WithRGB(colorR, colorG, colorB).WithA(colorA);
            return graphic;
        }

        /// Extension method for Graphic that sets the RGB color using a Color value, keeping the original alpha.
        /// Return this Graphic for method chaining.
        /// Arguments: Color color: Color value to set RGB channels.
        public static Graphic SetColorRGB(this Graphic graphic, Color color)
        {
            graphic.color = color.WithA(graphic.color.a);
            return graphic;
        }
    }
}
