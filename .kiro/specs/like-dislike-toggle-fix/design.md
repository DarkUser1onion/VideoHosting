# Like/Dislike Toggle Fix Design

## Overview

Исправление бага в механизме лайков/дизлайков видео-хостинг приложения. Основная проблема в том, что при toggle-механизме (повторное нажатие для снятия лайка/дизлайка) сервер возвращает статус 404 NotFound вместо 200 OK, что клиент воспринимает как ошибку. Это приводит к тому, что визуальное состояние кнопок не обновляется корректно.

## Glossary

- **Bug_Condition (C)**: Условие при котором toggle-механизм не работает - когда пользователь повторно нажимает на активный лайк/дизлайк
- **Property (P)**: Ожидаемое поведение - при toggle сервер должен возвращать 200 OK и клиент должен корректно обновлять UI
- **Preservation**: Существующее поведение при создании/переключении лайка и обработка ошибок должны остаться неизменными
- **SetLikeAsync**: Метод в `Server/Services/InteractionService.cs` который управляет лайками/дизлайками
- **LikesController**: Контроллер в `Server/Controllers/LikesController.cs` обрабатывающий API запросы для лайков
- **UserLiked**: Свойство в `Model/PlayerViewModel.cs` типа `bool?` которое хранит состояние лайка текущего пользователя (true=лайк, false=дизлайк, null=нет реакции)

## Bug Details

### Bug Condition

Баг проявляется когда пользователь повторно нажимает на уже активную кнопку лайка или дизлайка. Метод `SetLikeAsync` в `InteractionService.cs` возвращает `null` при удалении лайка, а контроллер `LikesController` преобразует это в `NotFound()` (404 статус). Клиент воспринимает 404 как ошибку и не обновляет визуальное состояние кнопок.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type LikeRequest (videoId, isLike)
  OUTPUT: boolean
  
  RETURN existsLikeFor(videoId, userId)
         AND existingLike.IsLike == input.isLike
         AND serverReturnsNotFound()
END FUNCTION
```

### Examples

- **Пример 1**: Пользователь имеет активный лайк (UserLiked = true), нажимает на кнопку лайка повторно
  - Ожидается: Лайк удаляется, UserLiked = null, кнопка не активна
  - Фактически: Сервер возвращает 404, клиент показывает ошибку, кнопка остаётся активной

- **Пример 2**: Пользователь имеет активный дизлайк (UserLiked = false), нажимает на кнопку дизлайка повторно
  - Ожидается: Дизлайк удаляется, UserLiked = null, кнопка не активна
  - Фактически: Сервер возвращает 404, клиент показывает ошибку, кнопка остаётся активной

- **Пример 3**: Пользователь переключает лайк на дизлайк
  - Ожидается: Лайк меняется на дизлайк, UserLiked = false
  - Фактически: Работает корректно (существующая логика)

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Создание нового лайка/дизлайка должно продолжать работать как раньше
- Переключение между лайком и дизлайком должно работать корректно
- Обработка ошибок аутентификации должна остаться неизменной
- Загрузка статуса лайка при открытии видео должна работать как раньше

**Scope:**
Все inputs которые NOT являются повторным нажатием на активный лайк/дизлайк должны быть полностью неизменны:
- Создание нового лайка/дизлайка
- Переключение между лайком и дизлайком
- Запрос статуса лайка
- Обработка сетевых ошибок

## Hypothesized Root Cause

На основе анализа кода выявлены следующие проблемы:

1. **Некорректный HTTP статус при toggle**: В `LikesController.cs` строка `if (like == null) return NotFound();` возвращает 404 при успешном удалении лайка через toggle
   - `null` от `SetLikeAsync` означает успешное удаление, а не ошибку
   - 404 статус семантически неверен - видео существует, лайк существует, операция успешна

2. **Неоднозначность возвращаемого значения**: `SetLikeAsync` возвращает `null` в двух разных случаях:
   - При успешном удалении лайка (toggle)
   - Потенциально при других сценариях

3. **Отсутствие чёткого контракта API**: Нет явного различия между "лайк удалён" и "лайк не найден"

## Correctness Properties

Property 1: Bug Condition - Toggle Like/Dislike Removal

_For any_ like/dislike action where the user clicks on an already active button (toggle scenario), the server SHALL return 200 OK status with an appropriate response indicating the like was removed, and the client SHALL update UserLiked to null, removing the visual highlight from the button.

**Validates: Requirements 2.3, 2.4, 2.5**

Property 2: Preservation - Non-Toggle Operations

_For any_ like/dislike action that is NOT a toggle (new like, switching between like and dislike), the fixed code SHALL produce exactly the same behavior as the original code, preserving all existing functionality for creating and switching likes/dislikes.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4**

## Fix Implementation

### Changes Required

**File**: `Server/Controllers/LikesController.cs`

**Function**: `SetLike`

**Specific Changes**:
1. **Изменить обработку null ответа**: Когда `SetLikeAsync` возвращает `null`, это означает успешное удаление лайка
   - Заменить `if (like == null) return NotFound();` на `if (like == null) return Ok(new { removed = true });`
   - Это даст понять клиенту, что лайк был успешно удалён

**Alternative Approach - Change Service Return Type**:
Можно изменить возвращаемый тип `SetLikeAsync` на более явный:

```csharp
public enum LikeOperationResult
{
    Created,
    Updated,
    Removed
}

public record LikeOperationResponse(LikeDto? Like, LikeOperationResult Result);
```

Но это потребует больше изменений, поэтому выбран первый подход.

**File**: `Services/ApiService.cs`

**Function**: `SetLikeAsync`

**Specific Changes**:
1. **Обработать новый формат ответа**: При получении `{ removed: true }` клиент должен понять, что лайк удалён
   - Текущая реализация уже возвращает `true` для любого успешного статуса
   - Нужно добавить логику для определения что именно произошло (создание/изменение/удаление)

**File**: `Model/PlayerViewModel.cs`

**Function**: `SetLike`

**Specific Changes**:
1. **Обновить UI после toggle**: После успешного `SetLikeAsync`, перезапросить статус лайка
   - Текущая реализация уже делает это: `UserLiked = await _api.GetLikeStatusAsync(Video.Id);`
   - Проблема в том, что при 404 статусе этот код не выполняется

### Minimal Fix Strategy

Минимальное изменение для исправления бага:

1. В `LikesController.cs` изменить строку 44:
   ```csharp
   // Было:
   if (like == null) return NotFound();
   
   // Станет:
   if (like == null) return Ok(new { removed = true });
   ```

2. В `ApiService.cs` добавить обработку ответа с `removed: true`:
   ```csharp
   public async Task<bool> SetLikeAsync(Guid videoId, bool isLike)
   {
       // ... existing code ...
       var responseContent = await response.Content.ReadAsStringAsync();
       
       // Проверяем, не является ли ответ указанием на удаление
       if (responseContent.Contains("removed"))
       {
           return true; // Лайк успешно удалён
       }
       
       return true; // Лайк создан/изменён
   }
   ```

3. В `PlayerViewModel.cs` метод `SetLike` уже правильно реализован - он перезапрашивает статус после успешной операции.

## Testing Strategy

### Validation Approach

Стратегия тестирования следует двухфазному подходу: сначала воспроизвести баг на текущем коде, затем проверить что исправление работает корректно и не ломает существующую функциональность.

### Exploratory Bug Condition Checking

**Goal**: Воспроизвести баг на текущем коде, подтвердив root cause анализ.

**Test Plan**: Написать тесты которые симулируют toggle операцию и проверяют что сервер возвращает неправильный статус.

**Test Cases**:
1. **Toggle Like Test**: Создать лайк, затем повторно нажать на него - проверить что сервер возвращает 404 (баг)
2. **Toggle Dislike Test**: Создать дизлайк, затем повторно нажать на него - проверить что сервер возвращает 404 (баг)
3. **Create Like Test**: Создать новый лайк - проверить что работает корректно (не баг)
4. **Switch Like to Dislike Test**: Иметь лайк, нажать дизлайк - проверить что работает корректно (не баг)

**Expected Counterexamples**:
- При toggle сервер возвращает 404 вместо 200
- Клиент показывает ошибку вместо обновления UI

### Fix Checking

**Goal**: Проверить что для всех inputs где выполняется bug condition, исправленная функция производит ожидаемое поведение.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := SetLike_fixed(input)
  ASSERT result.StatusCode == 200
  ASSERT client.UserLiked == null
END FOR
```

### Preservation Checking

**Goal**: Проверить что для всех inputs где bug condition NOT выполняется, исправленная функция производит тот же результат что и оригинальная.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT SetLike_original(input) = SetLike_fixed(input)
END FOR
```

**Test Cases**:
1. **Create Like Preservation**: Создание нового лайка должно работать как раньше
2. **Switch Preservation**: Переключение лайк->дизлайк и дизлайк->лайк должно работать как раньше
3. **Auth Error Preservation**: Попытка поставить лайк без авторизации должна показывать ту же ошибку
4. **Load Status Preservation**: Загрузка статуса лайка при открытии видео должна работать как раньше

### Unit Tests

- Тест `InteractionService.SetLikeAsync` для проверки всех сценариев (создание, переключение, удаление)
- Тест `LikesController.SetLike` для проверки правильных HTTP статусов
- Тест `ApiService.SetLikeAsync` для проверки обработки ответов

### Property-Based Tests

- Генерация случайных последовательностей лайк/дизлайк действий и проверка консистентности состояния
- Проверка инварианта: сумма лайков и дизлайков на сервере должна соответствовать реальным данным

### Integration Tests

- Полный цикл: создать лайк -> toggle (удалить) -> проверить что нет лайка
- Полный цикл: создать лайк -> переключить на дизлайк -> toggle -> проверить что нет реакции
- Проверка UI обновлений после каждой операции
