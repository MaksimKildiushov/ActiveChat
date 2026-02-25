using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ac.Data.Migrations.Api
{
    /// <summary>
    /// Триггер PostgreSQL: при INSERT или при UPDATE (статус в Pending) отправляет NOTIFY 'events' с Id записи.
    /// EventListenerService подписан на канал 'events' и обрабатывает события в реальном времени.
    /// Перед применением таблица public."Events" должна существовать (создайте её отдельной миграцией при необходимости).
    /// </summary>
    public partial class AddEventsNotifyTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION public.notify_events()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  IF TG_OP = 'INSERT' OR
     (TG_OP = 'UPDATE' AND (OLD.""Status"" IS DISTINCT FROM NEW.""Status"") AND NEW.""Status"" = 0)
  THEN
    PERFORM pg_notify('events', NEW.""Id""::text);
  END IF;
  RETURN NEW;
END;
$$;");

            migrationBuilder.Sql(@"
CREATE TRIGGER events_notify_trigger
  AFTER INSERT OR UPDATE OF ""Status""
  ON public.""Events""
  FOR EACH ROW
  EXECUTE PROCEDURE public.notify_events();
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS events_notify_trigger ON public.""Events"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS public.notify_events();");
        }
    }
}
