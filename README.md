# FactorySnap

**FactorySnap** to prosta aplikacja napisana na potrzeby studiów do monitorowania danych przemysłowych w czasie rzeczywistym.  
System łączy się z serwerem **OPC UA**, subskrybuje wskazane nody, zapisuje pomiary w bazie danych i prezentuje je na żywo w UI.

---

## ✨ Główne funkcje

- **Połączenie z OPC UA** i subskrypcja wielu nodów.
- **Live dashboard** z wykresami w czasie rzeczywistym.
- **Historia pomiarów** z możliwością wyświetlania okna czasowego.
- **Redis + SignalR** do dystrybucji danych live.
- **PostgreSQL (Timescale)** do przechowywania danych historycznych.

---

## 🧱 Architektura (skrót)

- **FactorySnap.Agent**  
  Łączy się z OPC UA, subskrybuje nody i publikuje pomiary.

- **FactorySnap.Api**  
  API do historii danych oraz konfiguracji OPC.  
  Zapewnia też SignalR (`/hubs/live`) do aktualizacji w UI.

- **FactorySnap.Client (Blazor WASM)**  
  Panel webowy z dashboardem i konfiguracją OPC.

- **FactorySnap.Shared**  
  Wspólne modele i kontrakty (DTO).

---

## 📈 Live Dashboard

Wykresy:

- pobierają dane historyczne z API,
- nasłuchują zmian w czasie rzeczywistym przez SignalR,
- pozwalają ustawić zakres czasu (np. 5 minut) suwakiem.

---

## 🧪 Status projektu

**Brak wersji release** — projekt jest w fazie rozwoju.

---

## ✅ Wymagania

Do uruchomienia lokalnie wystarczy:

- **uruchomiony Docker**
- **.NET + Aspire** (Aspire uruchamia wszystkie zależności automatycznie)

---

## ▶️ Uruchomienie

**Kroki:**

1. Upewnij się, że Docker działa.
2. Uruchom rozwiązanie przez **Aspire** (AppHost).  
   Aspire wystartuje wszystkie wymagane usługi.

---
