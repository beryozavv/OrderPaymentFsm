# Инструкции

## Ответы сервисов
Все сервисы, которые используются в конечном автомате, должны возвращать статус FsmResponseStatus, завернутый в ответ типа FsmServiceResponse или FsmServiceResponse<T>.
Это позволяет реализовать конечный автомат, в котором корректно будут обработаны трансиентные ошибки и вызваны повторные итерации и корректный переход в следующее состояние, если необходимо.