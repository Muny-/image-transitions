using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;

namespace image_transitions
{
    public class Gif
    {
        // Gdi+ constants absent from System.Drawing.
        const int PropertyTagFrameDelay = 0x5100;
        const int PropertyTagLoopCount = 0x5101;
        const short PropertyTagTypeLong = 4;
        const short PropertyTagTypeShort = 3;

        const int UintBytes = 4;

        ImageCodecInfo gifEncoder;
        PropertyItem frameDelay;
        PropertyItem loopPropertyItem;

        EncoderParameters encoderParams1;
        EncoderParameters encoderParamsN;
        EncoderParameters encoderParamsFlush;

        FileStream fStream;

        public Gif(string path, int num_frames)
        {

            gifEncoder = GetEncoder(ImageFormat.Gif);
            // Params of the first frame.
            encoderParams1 = new EncoderParameters(1);
            encoderParams1.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
            // Params of other frames.
            encoderParamsN = new EncoderParameters(1);
            encoderParamsN.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);
            // Params for the finalizing call.
            encoderParamsFlush = new EncoderParameters(1);
            encoderParamsFlush.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush);

            // PropertyItem for the frame delay (apparently, no other way to create a fresh instance).
            frameDelay = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            frameDelay.Id = PropertyTagFrameDelay;
            frameDelay.Type = PropertyTagTypeLong;
            // Length of the value in bytes.
            frameDelay.Len = num_frames * UintBytes;
            // The value is an array of 4-byte entries: one per frame.
            // Every entry is the frame delay in 1/100-s of a second, in little endian.
            frameDelay.Value = new byte[num_frames * UintBytes];
            // E.g., here, we're setting the delay of every frame to 1 second.
            var frameDelayBytes = BitConverter.GetBytes((uint)100);
            for (int j = 0; j < num_frames; ++j)
                Array.Copy(frameDelayBytes, 0, frameDelay.Value, j * UintBytes, UintBytes);

            // PropertyItem for the number of animation loops.
            loopPropertyItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            loopPropertyItem.Id = PropertyTagLoopCount;
            loopPropertyItem.Type = PropertyTagTypeShort;
            loopPropertyItem.Len = 1;
            // 0 means to animate forever.
            loopPropertyItem.Value = BitConverter.GetBytes((ushort)0);

            fStream = new FileStream(path, FileMode.Create);
        }

        bool first = true;
        Bitmap collection = null;

        public void AddFrame(Bitmap frame)
        {
            if (first)
            {
                collection = frame;
                collection.SetPropertyItem(frameDelay);
                collection.SetPropertyItem(loopPropertyItem);
                collection.Save(fStream, gifEncoder, encoderParams1);
                first = false;
            }
            else
            {
                collection.SaveAdd(frame, encoderParamsN);
            }
        }

        public void End()
        {
            collection.SaveAdd(encoderParamsFlush);
            fStream.Close();
        }

        static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }

            return null;
        }
    }
}