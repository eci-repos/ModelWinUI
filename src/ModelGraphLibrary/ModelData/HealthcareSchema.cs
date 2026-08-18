namespace ModelConsole.ModelData
{

   /// <summary>
   /// The 020 gate fixture (containerized per backlog 023): a hand-authored,
   /// deliberately messy grouped model in the healthcare clinic domain, written
   /// the way a third party would — the Entity / Elements / "Depends On"
   /// vocabulary, inconsistent type strings, an ambiguous name that exercises
   /// R7 precedence (declared beats inferred), and two dependencies to the same
   /// entity that need explicit roles. It uses the containerized
   /// Repository → Schema → Entities form (repository "Clinic", schema "clinic"),
   /// so the schema is declared once instead of on every entity. The shipped
   /// <c>Samples/Healthcare.json</c> is this exact document; the gate tests
   /// interpret it through the built-in grouped profile with no code updates to
   /// the renderers or explorer.
   /// </summary>
   public static class HealthcareSchema
   {
      /// <summary>
      /// The grouped JSON document, verbatim. This is the fixture the shipped
      /// file is kept in sync with
      /// (<c>SampleModelTests.ShippedJsonMatchesFixture</c>).
      /// </summary>
      public static string Json { get; } = """
         {
           "provenance": {
             "source": "clinic-schema.json",
             "version": "1.0",
             "loadedAt": "2026-08-18",
             "notes": "Third-party grouped model for the 020 gate: healthcare clinic domain, Entity / Elements / Depends On vocabulary, deliberately messy."
           },
           "metadata": {
             "domain": "healthcare",
             "owner": "clinic-it",
             "standard": "HL7-ish"
           },
           "enumerations": {
             "Gender": [ "M", "F", "OTHER" ],
             "VisitStatus": [ "SCHEDULED", "IN_PROGRESS", "COMPLETED", "CANCELLED" ],
             "ClaimStatus": [ "SUBMITTED", "PAID", "DENIED" ],
             "AppointmentStatus": [ "PENDING", "CONFIRMED", "NO_SHOW", "DONE" ]
           },
           "repository": "Clinic",
           "schemas": {
             "clinic": {
               "entities": {
                 "Patient": {
                   "description": "A person receiving care at the clinic.",
                   "metadata": { "sensitive": "true", "retention": "7y" },
                   "Elements": [
                     { "name": "id", "type": "int" },
                     { "name": "name", "type": "string", "description": "Full legal name." },
                     { "name": "dateOfBirth", "type": "date" },
                     { "name": "gender", "type": "string", "enum": "Gender" },
                     { "name": "phone", "type": "varchar" }
                   ]
                 },
                 "Provider": {
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "name", "type": "string" },
                     { "name": "specialty", "type": "string" },
                     { "name": "npi", "type": "string" }
                   ]
                 },
                 "Department": {
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "name", "type": "string" },
                     { "name": "location", "type": "string" }
                   ]
                 },
                 "Visit": {
                   "description": "A patient's encounter at the clinic.",
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "patient", "type": "int", "Depends On": "Patient", "cardinality": "1" },
                     { "name": "department", "type": "int", "Depends On": "Department", "cardinality": "0..1" },
                     { "name": "admittingProvider", "type": "int", "Depends On": "Provider", "childRole": "admitting", "parentRole": "admits" },
                     { "name": "attendingProvider", "type": "int", "Depends On": "Provider", "childRole": "attending", "parentRole": "attends" },
                     { "name": "visitDate", "type": "datetime" },
                     { "name": "reason", "type": "text" },
                     { "name": "status", "type": "string", "enum": "VisitStatus" }
                   ]
                 },
                 "VisitProvider": {
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "visit", "type": "int", "Depends On": "Visit" },
                     { "name": "provider", "type": "int", "Depends On": "Provider" },
                     { "name": "role", "type": "string" }
                   ]
                 },
                 "Diagnosis": {
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "code", "type": "string" },
                     { "name": "description", "type": "text" }
                   ]
                 },
                 "VisitDiagnosis": {
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "visit", "type": "int", "Depends On": "Visit" },
                     { "name": "diagnosis", "type": "int", "Depends On": "Diagnosis" },
                     { "name": "isPrimary", "type": "bool" }
                   ]
                 },
                 "Medication": {
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "name", "type": "string" },
                     { "name": "strength", "type": "string" },
                     { "name": "form", "type": "string" }
                   ]
                 },
                 "Prescription": {
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "visit", "type": "int", "Depends On": "Visit" },
                     { "name": "medication", "type": "int", "Depends On": "Medication" },
                     { "name": "dosage", "type": "string" },
                     { "name": "quantity", "type": "int" },
                     { "name": "refills", "type": "integer" }
                   ]
                 },
                 "Insurance": {
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "name", "type": "string" },
                     { "name": "plan", "type": "string" }
                   ]
                 },
                 "Claim": {
                   "description": "An insurance claim submitted for a visit.",
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "visit", "type": "int", "Depends On": "Visit" },
                     { "name": "insurance", "type": "int", "Depends On": "Insurance" },
                     { "name": "PatientId", "type": "int", "Depends On": "Visit" },
                     { "name": "amount", "type": "decimal" },
                     { "name": "status", "type": "string", "enum": "ClaimStatus" }
                   ]
                 },
                 "Appointment": {
                   "description": "A scheduled patient-provider appointment.",
                   "Elements": [
                     { "name": "id", "type": "int", "primaryKey": true },
                     { "name": "patient", "type": "int", "Depends On": "Patient" },
                     { "name": "provider", "type": "int", "Depends On": "Provider" },
                     { "name": "department", "type": "int", "Depends On": "Department" },
                     { "name": "scheduledAt", "type": "datetime" },
                     { "name": "status", "type": "string", "enum": "AppointmentStatus" }
                   ]
                 }
               }
             }
           }
         }
         """;
   }

}
