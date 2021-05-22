using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.IO.Compression;
using System.ComponentModel.DataAnnotations;

namespace weatherapi.Controllers
{
    [Route("[controller]")]
    public class CovidQRCodeController : ControllerBase
    {
        private readonly ILogger<CovidQRCodeController> _logger;

        public CovidQRCodeController(ILogger<CovidQRCodeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public  ActionResult GetSHCdata(string value = "")
        {
            if (value ==null || string.IsNullOrWhiteSpace(value))
                return NotFound();

            Console.WriteLine($"sch.covidQRData: {value}");

            var shc_data = CovideQRCodedecoder(value);

            if (!string.IsNullOrWhiteSpace(shc_data))
            {
                return Ok(shc_data);
            }
            else
            {
                return NotFound();
            }
        }



        private string CovideQRCodedecoder(string qr_data)
        {
            if (qr_data == null || string.IsNullOrWhiteSpace(qr_data))
                return string.Empty;

            var toks = qr_data.Split('/');
            if (toks.Length != 2 || !toks[0].ToLower().Equals("shc:"))
                return string.Empty;

            var jws = "";

            Regex regex = new Regex("..");
            foreach (Match match in regex.Matches(toks[1]))
            {
                string char_code = Convert.ToChar(int.Parse(match.ToString()) + 45).ToString();
                //Console.WriteLine(match.Value + "=> char code = " + char_code);
                jws += Convert.ToChar(int.Parse(match.ToString()) + 45).ToString();
            }

            // Console.WriteLine($"JWS: {jws}"); 

            var parts = jws.Split('.');

            var jws_parts = new List<byte[]>();

            foreach(var p in parts)
            {
                // Console.WriteLine($"JWS part    : {p}"); 
                var dec_part = decode(p);
                jws_parts.Add(dec_part);
            }

            Console.WriteLine($"JWS Header:");
            Console.WriteLine(Encoding.UTF8.GetString(jws_parts[0]));  

            var shc_data = helper.Decompress(jws_parts[1]);

            Console.WriteLine($"SHC Data:");
            Console.WriteLine(Encoding.UTF8.GetString(shc_data));  

            return Encoding.UTF8.GetString(shc_data);
        }

        private byte[] decode(string data)
        {
            var ret = data;
            var missing_padding = data.Length % 4;
            switch(missing_padding) {
                case 1: ret += "==="; break;
                case 2: ret += "=="; break;
                case 3: ret += "="; break;
            }
            //ret.Replace('_', '/').Replace('-', '+');
            //Console.WriteLine($"Decode(d) : {ret}");
            var ret2 = Base64UrlEncoder.DecodeBytes(ret);
            //Console.WriteLine($"Decode(d2) : {Encoding.ASCII.GetString(ret2)}");
            return ret2; 
        }
    }

    public static class helper
    {
        public static byte[] Decompress(byte[] data)
        {
            var outputStream = new MemoryStream();
            using (var compressedStream = new MemoryStream(data))
            using (var inputStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
            {
                inputStream.CopyTo(outputStream);
                outputStream.Position = 0;
                //return outputStream;
            }
            // convert to byte[]
            var byteArray = new byte[outputStream.Length];
            var count = outputStream.Read(byteArray, 0, 20);
            while(count < outputStream.Length)
            {
                byteArray[count++] = Convert.ToByte(outputStream.ReadByte());
            }
            return byteArray;                       
        }
    }
}
