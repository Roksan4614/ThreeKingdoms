# 서버 연동 test 브랜치 검토

이 브랜치는 기존 Unity 화면을 실제 API와 DEV MySQL·Valkey에 연결하는 프로토타입이다. 서버와 클라이언트 모두 `test`에서 작업하며, 클라이언트 개발자가 검토한 뒤 `master` 반영 여부를 결정한다.

- 서버 구현: `threekingz-io/threekingz-server`의 `test` (`4cdef23`).
- 생성 계약: `work/shared`의 `9e36773`. API 서버 소스에서 생성한 C# 253개, API 메서드 50개, 테이블 53개다.
- 실제 검증 계정: DEV UID **29 / UnityTest29**. 플레이·영수증·원장은 삭제하지 않고 보존했다.

## 실행

1. 서버 저장소의 `test`에서 `npm.cmd run configure:client`를 실행한다. 이 명령은 Unity의 ignored `UserSettings/ServerIntegration.json`을 설정하며 서버를 추가로 실행하지 않는다. 새 검증 계정이 필요한 환경만 `npm.cmd run configure:client -- --prepare`를 사용한다.
2. 서버가 실행 중이면 기존 프로세스를 사용한다. 꺼져 있을 때만 VS Code `[Dev]Launch All Profiles`를 실행한다. API `11080`, Raid `11083`, Scheduler `11086`을 포함한 8개 역할과 기존 핫 리로드를 사용한다.
3. Unity **6000.3.5f2**에서 `Assets/_Core/Scenes/00_Boot.unity`를 열고 Play한다. DEV 테스트 로그인 후 기존 계정의 지역·재화·무장·영지가 복원된다.

API는 `http://127.0.0.1:11080`, 테이블은 같은 서버의 `/table_data/json/`, Raid는 `ws://127.0.0.1:11083/`다. 테이블 manifest와 각 JSON의 SHA-256을 확인하며 API 요청에 같은 테이블 버전을 전달한다. 테이블을 변경하면 Boot부터 다시 시작한다. 실제 API 키와 세션은 커밋하지 않는다.

## 확인한 동작

| 영역 | 실제 Unity·서버 확인 |
|---|---|
| 시작 | DEV 로그인, 위 지역 선택, 재로그인 복원, 실제 연결 실패 후 다시 시도 |
| 무장 | 조조 성장 1, 명장 승급, 특성 변경·잠금·해제, 유물 1강, 직위 배정 |
| 재화·아이템 | 일일 상점 구매, 군량 주머니 개봉, 보물 조각 상자 개봉→묵자 획득·장착, 절대 잔액·재고 적용 |
| 영지 | 농지 배치·수확, 궁성으로 이동 배치, 궁성 1→2 증축 완료, 관아 임무·보상 |
| 가이드 | 실제 이동·기본 공격·스킬·대시 후 서버 보상 수령 |
| 요일던전 | 실제 입장·미처치 결과 정산, 횟수 차감, 결과 창 종료, 추가 입장 처리 |
| 토너먼트 | 편성·후보·순위와 실제 전투 `battle_id=19`; 승리, 점수 +5, 포인트 +100 |
| 레이드 | 공용 회차 `34` 기본·진 여포 처치, 피해 `99,804`, 1위, `1,400P`; 소켓 재접속 복구 |

시드 재화와 개봉용 상자는 전용 계정의 검증 재료다. 전투 승패·피해·보물 소유·장착을 DB에 강제로 넣어 성공을 만들지 않았다. Unity 입력 fixture도 실제 버튼·이동 이벤트를 전달하며 전투 결과를 주입하지 않는다.

## 구현 위치와 검토 기준

- `Assets/_Core/Scripts/Shared`: 서버 생성물이므로 직접 수정하지 않는다. 서버에서 검증·일반 push한 `work/shared`를 통합한다.
- `Assets/_Core/Scripts/Server`: 실제 HTTP·세션·헤더·취소 처리, 서버 상태 적용, 캐릭터 요청, Raid 전송을 담당한다.
- 기존 화면의 `*.Server.cs`: 기존 UI 이벤트를 서버 요청에 연결한다. 실패 후 로컬에서 보상을 지급하지 않으며 재시도는 같은 요청 ID를 유지한다.
- Raid 미확정 피해는 계정별로 UUID와 본문을 보존한다. 서버 성공 또는 명시적 거절만 제거하고, 참가 회차 ID로 종료 상태까지 복원한다.
- Editor 전용 검증 도구는 `Temp/ServerIntegrationProof/request.json`의 명시 요청을 한 번만 처리한다. fixture는 현재 Play·UID 29·로컬 API를 확인하고 임의 로그인이나 강제 승패 처리를 하지 않는다.

서버 검사 849개, 실제 Newtonsoft.Json·UniTask 생성물 검증, Unity Editor 컴파일·Play, DB/Redis 대조를 구분해 확인했다. 재시도·전투 간격·Raid 요청 보존과 WebGL 전송 어댑터도 별도 회귀 검사했다. 구체적인 결과와 요청 ID는 검증 증거에 기록한다.

- [실제 플레이·DB/Redis 42개 검사 결과](evidence/server-integration-20260913/verification.json)
- [레이드 처치·정산 화면](evidence/server-integration-20260913/raid-result.png), [실제 잔액·최근 참가 표시](evidence/server-integration-20260913/raid-lobby.png)
- [보상 표시 회귀](evidence/server-integration-20260913/reward-result.png), [요일던전 미처치 결과](evidence/server-integration-20260913/daily-result.png)

실플레이에서 발견한 지역 직책 매핑, 재시도 요청 ID, 영지 revision, 증축 공용 테이블 행, 가이드 입력 관측, 공격 주기 단위·타격 전 애니메이션 재시작, 요일던전 타이머·중복 정산 창, 보상 창 초기화, Boot 조명·EventSystem 중복을 수정했다. 최신 재시도·플레이 구간에서는 예외나 중복 시스템 경고가 없고, 영속 전역 조명과 EventSystem은 각각 1개다.

## 프로토타입 경계

이번 플레이 검증은 Unity Editor에서 수행했다. WebGL은 브라우저 WebSocket 어댑터와 Player 심볼 컴파일·콜백 계약을 검증했으며 실제 WebGL 빌드·브라우저 플레이를 완료했다고 보지 않는다. Web 플랫폼에서 .NET 소켓을 직접 사용하지 않는 이유는 [Unity 6.3 Web networking 계약](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-networking.html)에 따른다.

실결제·광고 SDK·출시용 인증과 운영 보안, 신규 스토리 서버, 추가 레이드 운영 기능은 후속 범위다. 서버 API가 없는 기존 콘텐츠는 준비 중으로 표시한다. 기존 `ThreeKingdoms.slnx`의 사용자 변경은 이번 커밋에서 제외한다.
