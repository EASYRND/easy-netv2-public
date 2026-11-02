using System;
using System.Linq;
using Easy.NetV2;

namespace Easy.NetV2.ConsoleSample
{
    internal static class Program
    {
        // Replace these example addresses with addresses from the device map Excel file.
        private const uint SingleValueAddress = 0x00010000;
        private const uint EnableAddress = 0x00010004;
        private const uint FrequencyAddress = 0x00010008;

        private static int Main(string[] args)
        {
            string ip = args.Length > 0 ? args[0] : "192.168.0.10";
            bool enableWrite = args.Length > 1 &&
                string.Equals(args[1], "--write", StringComparison.OrdinalIgnoreCase);

            using (var tcp = new EasyProtocolTcp(ip))
            {
                try
                {
                    tcp.ConnectionChanged += (sender, e) =>
                        Console.WriteLine("연결 상태 변경: " + e.Type);

                    Console.WriteLine("연결 중: {0}:6313", ip);
                    if (!tcp.Open())
                    {
                        Console.WriteLine("장치 연결에 실패했습니다.");
                        return 1;
                    }

                    Console.WriteLine("연결 상태: " + (tcp.IsOpened ? "연결됨" : "연결되지 않음"));
                    tcp.Timeout = 1500;

                    if (enableWrite)
                    {
                        WriteSingle(tcp);
                        WriteGroup(tcp);
                    }
                    else
                    {
                        Console.WriteLine("쓰기 예제는 --write 옵션이 있을 때만 실행됩니다.");
                    }

                    ReadSingle(tcp);
                    ReadGroup(tcp);
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("통신 오류: " + ex.Message);
                    return 1;
                }
                finally
                {
                    tcp.Close();
                    Console.WriteLine("연결을 종료했습니다.");
                }
            }
        }

        private static void WriteSingle(EasyProtocolTcp tcp)
        {
            EasyReturnStatus status = tcp.Write(SingleValueAddress, (uint)1234);
            Console.WriteLine(status == EasyReturnStatus.Success
                ? "단일 쓰기 성공"
                : "단일 쓰기 실패: " + status);
        }

        private static void ReadSingle(EasyProtocolTcp tcp)
        {
            byte[] data;
            EasyReturnStatus status = tcp.Read(SingleValueAddress, 4, out data);
            if (status != EasyReturnStatus.Success)
            {
                Console.WriteLine("단일 읽기 실패: " + status);
                return;
            }

            Console.WriteLine("단일 읽기 값: " + BitConverter.ToUInt32(data, 0));
        }

        private static void WriteGroup(EasyProtocolTcp tcp)
        {
            var request = new EasyProtocolV0(true);
            request.SetGroup(EnableAddress, 1, new byte[] { 1 });
            request.SetGroup(FrequencyAddress, 4, BitConverter.GetBytes((uint)1000));

            EasyProtocolGroup response = tcp.Send(request);
            PrintGroupResponse(tcp, response, "그룹 쓰기");
        }

        private static void ReadGroup(EasyProtocolTcp tcp)
        {
            var request = new EasyProtocolV0();
            request.SetGroup(EnableAddress, 1, null);
            request.SetGroup(FrequencyAddress, 4, null);

            EasyProtocolGroup response = tcp.Send(request);
            if (!IsSuccessfulGroupResponse(tcp, response, "그룹 읽기"))
            {
                return;
            }

            if (response.DataSize < 5)
            {
                Console.WriteLine("그룹 읽기 응답 데이터가 부족합니다: " + response.DataSize);
                return;
            }

            byte enable = response.RecvBuffer(1).ToArray()[0];
            uint frequency = BitConverter.ToUInt32(response.RecvBuffer(4).ToArray(), 0);
            Console.WriteLine("그룹 읽기 값: Enable={0}, Frequency={1}", enable, frequency);
        }

        private static void PrintGroupResponse(
            EasyProtocolTcp tcp,
            EasyProtocolGroup response,
            string operation)
        {
            if (IsSuccessfulGroupResponse(tcp, response, operation))
            {
                Console.WriteLine(operation + " 성공");
            }
        }

        private static bool IsSuccessfulGroupResponse(
            EasyProtocolTcp tcp,
            EasyProtocolGroup response,
            string operation)
        {
            if (response == null)
            {
                Console.WriteLine(operation + " 전송 실패: " + tcp.LastSendStatus);
                return false;
            }

            if (response.Command == 3)
            {
                Console.WriteLine(operation + " 장치 오류: " +
                    (EasyReturnStatus)response.Status);
                return false;
            }

            if (response.Command != 2)
            {
                Console.WriteLine(operation + " 예상하지 못한 응답: " + response.Command);
                return false;
            }

            return true;
        }
    }
}
