# Easy.NetV2

Easy.NetV2는 TCP(기본 포트 `6313`)를 통해 장치와 Easy Protocol 통신을 하는 C# 라이브러리입니다. 이 공개 저장소에는 프로토콜 라이브러리와 간단한 콘솔 예제가 포함되어 있습니다. 사내 WinForms 관리 UI와 데모 화면은 포함하지 않습니다.

- 대상 프레임워크: .NET Framework 4.7.2
- 주요 클래스: `EasyProtocolTcp`
- 예제 프로젝트: [`samples/Easy.NetV2.ConsoleSample`](samples/Easy.NetV2.ConsoleSample)

## 시작하기

Visual Studio 또는 .NET Framework 4.7.2 개발 도구가 설치된 환경에서 `src/Easy.NetV2/Easy.NetV2.csproj`를 참조합니다. 예제 콘솔 프로젝트는 라이브러리 프로젝트를 직접 참조합니다.

장치의 IP 주소와 레지스터 정보는 장치와 함께 제공되는 주소 맵 Excel 파일에서 확인하세요. 주소, 데이터 크기, 타입, 부호 여부와 쓰기 가능 여부를 Excel 기준으로 맞춰야 합니다. 아래 주소는 설명을 위한 예시이며, 실제 장치 값으로 바꾸어야 합니다. 쓰기 요청은 실제 장치 설정에 영향을 줄 수 있습니다.

## 1. 장치 연결하기

`EasyProtocolTcp` 생성자에 장치 IP 주소를 전달하고 `Open()`을 호출합니다. 기본 포트는 `6313`입니다.

```csharp
using System;
using Easy.NetV2;

using (var tcp = new EasyProtocolTcp("192.168.0.10"))
{
    try
    {
        if (!tcp.Open())
        {
            Console.WriteLine("장치 연결에 실패했습니다.");
            return;
        }

        Console.WriteLine("장치에 연결되었습니다.");
        // 연결 후 아래 읽기/쓰기 코드를 실행합니다.
    }
    catch (Exception ex)
    {
        Console.WriteLine("연결 또는 통신 오류: " + ex.Message);
    }
    finally
    {
        tcp.Close();
    }
}
```

`using` 블록이 끝나면 `Dispose()`가 호출되어 자원이 정리됩니다. `finally`의 `Close()`는 통신 중 예외가 발생해도 연결을 종료하도록 합니다.

## 2. 연결 상태 확인하기

`IsOpened` 속성은 TCP 연결 상태를 확인합니다. 연결 상태 변경 이벤트를 구독할 수도 있습니다.

```csharp
tcp.ConnectionChanged += (sender, e) =>
    Console.WriteLine("연결 상태 변경: " + e.Type);

Console.WriteLine(tcp.IsOpened ? "연결됨" : "연결되지 않음");
```

`IsOpened`는 TCP 연결 여부를 나타내며, 장치의 요청 응답 여부까지 보장하지는 않습니다. 실제 통신 결과는 각 읽기/쓰기의 반환 상태로 확인하세요.

## 3. 연결 끊기

통신이 끝나면 `Close()`를 호출합니다. `Close()` 이후 `IsOpened`로 상태를 확인할 수 있습니다.

```csharp
tcp.Close();
Console.WriteLine(tcp.IsOpened ? "연결 중" : "연결 종료");
```

## 4. 값 쓰기 (단일)

`Write<T>(주소, 값)`으로 단일 값을 씁니다. 전달하는 C# 타입으로 데이터 크기가 결정됩니다. 예를 들어 `uint`는 4바이트입니다.

```csharp
uint address = 0x00010000; // Excel 주소 맵의 실제 쓰기 가능 주소로 변경
uint value = 1234;

EasyReturnStatus status = tcp.Write(address, value);
if (status == EasyReturnStatus.Success)
    Console.WriteLine("쓰기 성공");
else
    Console.WriteLine("쓰기 실패: " + status);
```

지원 타입은 `byte`, `short`, `ushort`, `int`, `uint`, `double`, `string` 및 값 형식 구조체입니다. 주소 맵의 타입과 크기에 맞는 C# 타입을 사용하세요.

## 5. 값 읽기 (단일)

`Read(주소, 바이트 수, out 데이터)`로 지정한 크기의 데이터를 읽습니다. 성공 상태를 확인한 뒤 Excel 주소 맵에 맞는 타입으로 변환합니다.

```csharp
uint address = 0x00010000; // Excel 주소 맵의 실제 주소로 변경
byte[] data;

EasyReturnStatus status = tcp.Read(address, 4, out data);
if (status == EasyReturnStatus.Success)
{
    uint value = BitConverter.ToUInt32(data, 0);
    Console.WriteLine("읽은 값: " + value);
}
else
{
    Console.WriteLine("읽기 실패: " + status);
}
```

4바이트 부호 있는 값은 `BitConverter.ToInt32(data, 0)`, 4바이트 부호 없는 값은 `BitConverter.ToUInt32(data, 0)`을 사용합니다. 1바이트 값은 `data[0]`으로 확인할 수 있습니다.

## 6. 값 쓰기 (그룹)

여러 주소에 한 번에 쓰려면 `EasyProtocolV0(true)` 요청에 주소별 데이터를 순서대로 추가합니다. 응답은 `EasyProtocolGroup`으로 받습니다.

```csharp
uint enableAddress = 0x00010004; // Excel 주소 맵의 실제 쓰기 가능 주소로 변경
uint frequencyAddress = 0x00010008; // Excel 주소 맵의 실제 쓰기 가능 주소로 변경

var request = new EasyProtocolV0(true); // true: 쓰기
request.SetGroup(enableAddress, 1, new byte[] { 1 });
request.SetGroup(frequencyAddress, 4, BitConverter.GetBytes((uint)1000));

EasyProtocolGroup response = tcp.Send(request);
if (response == null)
{
    Console.WriteLine("그룹 쓰기 전송 실패: " + tcp.LastSendStatus);
}
else if (response.Command == 3)
{
    Console.WriteLine("장치 오류: " + (EasyReturnStatus)response.Status);
}
else if (response.Command == 2)
{
    Console.WriteLine("그룹 쓰기 성공");
}
else
{
    Console.WriteLine("예상하지 못한 응답: " + response.Command);
}
```

그룹 쓰기는 각 레지스터의 쓰기 가능 여부와 값의 범위를 Excel 주소 맵에서 확인한 뒤 사용하세요.

## 7. 값 읽기 (그룹)

읽기 요청은 `EasyProtocolV0()`로 만들고, 읽을 주소와 크기를 순서대로 추가합니다. 응답 데이터도 같은 순서로 읽습니다.

```csharp
using System.Linq;

uint enableAddress = 0x00010004; // Excel 주소 맵의 실제 주소로 변경
uint frequencyAddress = 0x00010008; // Excel 주소 맵의 실제 주소로 변경

var request = new EasyProtocolV0(); // 기본값: 읽기
request.SetGroup(enableAddress, 1, null);
request.SetGroup(frequencyAddress, 4, null);

EasyProtocolGroup response = tcp.Send(request);
if (response == null)
{
    Console.WriteLine("그룹 읽기 전송 실패: " + tcp.LastSendStatus);
}
else if (response.Command == 3)
{
    Console.WriteLine("장치 오류: " + (EasyReturnStatus)response.Status);
}
else if (response.Command != 2 || response.DataSize < 5)
{
    Console.WriteLine("응답 Command 또는 데이터 크기가 올바르지 않습니다.");
}
else
{
    byte enable = response.RecvBuffer(1).ToArray()[0];
    uint frequency = BitConverter.ToUInt32(response.RecvBuffer(4).ToArray(), 0);
    Console.WriteLine("Enable={0}, Frequency={1}", enable, frequency);
}
```

`RecvBuffer()`는 호출할 때마다 응답의 다음 위치로 이동합니다. 요청에 추가한 순서와 데이터 크기에 맞춰 읽으세요.

## 콘솔 예제 실행

예제는 기본으로 읽기만 수행합니다. 쓰기를 실행할 때는 `--write` 옵션을 명시해야 합니다. 먼저 `Program.cs`의 예시 주소를 Excel 주소 맵의 주소로 바꾸고, 쓰기 대상이 안전한지 확인하세요.

```text
Easy.NetV2.ConsoleSample.exe 192.168.0.10
Easy.NetV2.ConsoleSample.exe 192.168.0.10 --write
```

프로젝트 빌드에는 .NET Framework 4.7.2 타깃팅 팩이 필요합니다. Visual Studio 또는 MSBuild 개발자 명령 프롬프트에서 저장소 루트 기준으로 다음과 같이 빌드할 수 있습니다.

```text
msbuild samples\Easy.NetV2.ConsoleSample\Easy.NetV2.ConsoleSample.csproj /p:Configuration=Release
```

## 라이선스

현재 저장소에는 라이선스 파일이 없습니다. 공개 저장소에 접근할 수 있다는 사실만으로 코드의 재사용·수정·재배포 권한이 부여되는 것은 아니므로, 별도 라이선스가 안내되기 전에는 권리자에게 사용 조건을 확인하세요.
