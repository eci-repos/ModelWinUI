using System;
using System.Collections.Generic;

using Model.Data;

namespace ModelConsole.ModelData
{

   /// <summary>
   /// Sample public-safety / criminal-justice schema used to exercise the
   /// renderer and the routing tests. Exactly 50 tables and 74 FK edges.
   /// Domain areas: Identity, Reference data, Agencies &amp; personnel,
   /// Geography &amp; facilities, Incidents &amp; dispatch, Enforcement,
   /// Offenses &amp; case, Courts &amp; sentencing.
   /// </summary>
   public static class PublicSafetySchema
   {

      private const string SCHEMA = "PublicSafety";

      /// <summary>
      /// The full schema, in a deterministic order (grouped by domain area).
      /// </summary>
      public static TableInfo[] Tables { get; } = BuildTables();

      private static TableInfo[] BuildTables()
      {
         var tables = new List<TableInfo>();

         // -- Identity (7) ------------------------------------------------
         tables.Add(T("Person",
            C("PersonID", 20, key: true),
            C("SexCode", 20, fk: "RefSex"),
            C("RaceCode", 20, fk: "RefRace"),
            C("BirthLocationID", 20, fk: "Address"),
            C("EthnicityCode", 20),
            C("BirthDate", type: "DATETIMEOFFSET"),
            C("DeathDate", type: "DATETIMEOFFSET"),
            C("NationalityCode", 20),
            C("IdentificationNumber", 40),
            C("DriverLicenseNumber", 40),
            C("SocialSecurityID", 20)));

         tables.Add(T("PersonAlias",
            C("AliasID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("AliasTypeID", 20, fk: "RefIdentifierType"),
            C("AliasName", 60),
            C("AliasDate", type: "DATETIMEOFFSET")));

         tables.Add(T("PersonName",
            C("PersonNameID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("NameTypeID", 20, fk: "RefIdentifierType"),
            C("NameGiven", 40),
            C("NameMiddle", 40),
            C("NameSurname", 40),
            C("NamePrefix", 10),
            C("NameSuffix", 10),
            C("NameFull", 128)));

         tables.Add(T("PersonAddress",
            C("PersonAddressID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("AddressID", 20, fk: "Address"),
            C("AddressTypeID", 20, fk: "RefIdentifierType"),
            C("EffectiveDate", type: "DATETIMEOFFSET"),
            C("EndDate", type: "DATETIMEOFFSET")));

         tables.Add(T("PersonContact",
            C("ContactID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("ContactTypeID", 20, fk: "RefIdentifierType"),
            C("ContactValue", 80)));

         tables.Add(T("PersonIdentifier",
            C("IdentifierID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("IdentifierTypeID", 20, fk: "RefIdentifierType"),
            C("IdentifierValue", 40)));

         tables.Add(T("PersonPhysicalFeature",
            C("FeatureID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("FeatureType", 40),
            C("FeatureValue", 60)));

         // -- Reference data (6) -----------------------------------------
         tables.Add(T("RefIdentifierType",
            C("TypeID", 20, key: true),
            C("Description", 80)));

         tables.Add(T("RefIncidentType",
            C("TypeID", 20, key: true),
            C("Description", 80)));

         tables.Add(T("RefIncidentRole",
            C("RoleID", 20, key: true),
            C("Description", 80)));

         tables.Add(T("RefRace",
            C("RaceCode", 20, key: true),
            C("Description", 60)));

         tables.Add(T("RefSex",
            C("SexCode", 20, key: true),
            C("Description", 20)));

         tables.Add(T("RefChargeSeverity",
            C("SeverityID", 20, key: true),
            C("Description", 60)));

         // -- Agencies & personnel (7) ------------------------------------
         tables.Add(T("Agency",
            C("AgencyID", 20, key: true),
            C("AgencyName", 80),
            C("AgencyType", 40),
            C("JurisdictionID", 20, fk: "Jurisdiction")));

         tables.Add(T("AgencyUnit",
            C("AgencyUnitID", 20, key: true),
            C("AgencyID", 20, fk: "Agency"),
            C("UnitName", 60),
            C("UnitType", 40)));

         tables.Add(T("Employee",
            C("EmployeeID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("AgencyID", 20, fk: "Agency"),
            C("EmployeeNumber", 20),
            C("Title", 60),
            C("HireDate", type: "DATETIMEOFFSET")));

         tables.Add(T("EmployeeAssignment",
            C("AssignmentID", 20, key: true),
            C("EmployeeID", 20, fk: "Employee"),
            C("AgencyUnitID", 20, fk: "AgencyUnit"),
            C("AssignmentType", 40),
            C("AssignmentDate", type: "DATETIMEOFFSET"),
            C("EndDate", type: "DATETIMEOFFSET")));

         tables.Add(T("EmployeeCertification",
            C("CertificationID", 20, key: true),
            C("EmployeeID", 20, fk: "Employee"),
            C("CertificationType", 60),
            C("CertificationDate", type: "DATETIMEOFFSET"),
            C("ExpirationDate", type: "DATETIMEOFFSET")));

         tables.Add(T("EmployeeContact",
            C("EmployeeContactID", 20, key: true),
            C("EmployeeID", 20, fk: "Employee"),
            C("ContactTypeID", 20, fk: "RefIdentifierType"),
            C("ContactValue", 80)));

         tables.Add(T("EmployeeTraining",
            C("TrainingID", 20, key: true),
            C("EmployeeID", 20, fk: "Employee"),
            C("TrainingType", 60),
            C("CompletionDate", type: "DATETIMEOFFSET")));

         // -- Geography & facilities (5) ----------------------------------
         tables.Add(T("Jurisdiction",
            C("JurisdictionID", 20, key: true),
            C("JurisdictionName", 80),
            C("JurisdictionType", 40),
            C("CountyCode", 20)));

         tables.Add(T("Address",
            C("AddressID", 20, key: true),
            C("GeographicAreaID", 20, fk: "GeographicArea"),
            C("JurisdictionID", 20, fk: "Jurisdiction"),
            C("StreetNumber", 20),
            C("StreetName", 80),
            C("City", 60),
            C("StateCode", 10),
            C("PostalCode", 10)));

         tables.Add(T("GeographicArea",
            C("GeographicAreaID", 20, key: true),
            C("AreaName", 80),
            C("AreaType", 40)));

         tables.Add(T("Facility",
            C("FacilityID", 20, key: true),
            C("AgencyID", 20, fk: "Agency"),
            C("JurisdictionID", 20, fk: "Jurisdiction"),
            C("FacilityName", 80),
            C("FacilityType", 40)));

         tables.Add(T("FacilitySection",
            C("SectionID", 20, key: true),
            C("FacilityID", 20, fk: "Facility"),
            C("SectionName", 60),
            C("SectionType", 40)));

         // -- Incidents & dispatch (7) ------------------------------------
         tables.Add(T("Incident",
            C("IncidentID", 20, key: true),
            C("IncidentTypeID", 20, fk: "RefIncidentType"),
            C("JurisdictionID", 20, fk: "Jurisdiction"),
            C("ReportingOfficerID", 20, fk: "Employee"),
            C("IncidentDate", type: "DATETIMEOFFSET"),
            C("LocationDescription", 128),
            C("Status", 20)));

         tables.Add(T("IncidentParticipant",
            C("ParticipantID", 20, key: true),
            C("IncidentID", 20, fk: "Incident"),
            C("PersonID", 20, fk: "Person"),
            C("RoleID", 20, fk: "RefIncidentRole"),
            C("ParticipantNotes", 128)));

         tables.Add(T("IncidentVehicle",
            C("IncidentVehicleID", 20, key: true),
            C("IncidentID", 20, fk: "Incident"),
            C("PlateNumber", 20),
            C("PlateStateCode", 10),
            C("VehicleMake", 40),
            C("VehicleModel", 40),
            C("VehicleYear", 4, type: "INT")));

         tables.Add(T("IncidentProperty",
            C("PropertyID", 20, key: true),
            C("IncidentID", 20, fk: "Incident"),
            C("PropertyType", 40),
            C("PropertyDescription", 128),
            C("RecoveredDate", type: "DATETIMEOFFSET")));

         tables.Add(T("IncidentNarrative",
            C("NarrativeID", 20, key: true),
            C("IncidentID", 20, fk: "Incident"),
            C("NarrativeType", 40),
            C("NarrativeText", 512)));

         tables.Add(T("DispatchCall",
            C("CallID", 20, key: true),
            C("IncidentID", 20, fk: "Incident"),
            C("JurisdictionID", 20, fk: "Jurisdiction"),
            C("CallType", 40),
            C("CallStatus", 20),
            C("CallDateTime", type: "DATETIMEOFFSET")));

         tables.Add(T("DispatchUnit",
            C("DispatchUnitID", 20, key: true),
            C("CallID", 20, fk: "DispatchCall"),
            C("UnitIdentifier", 40),
            C("DispatchedDate", type: "DATETIMEOFFSET"),
            C("ArrivedDate", type: "DATETIMEOFFSET")));

         // -- Enforcement (6) ---------------------------------------------
         tables.Add(T("Arrest",
            C("ArrestID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("ArrestingOfficerID", 20, fk: "Employee"),
            C("JurisdictionID", 20, fk: "Jurisdiction"),
            C("ArrestNumber", 20),
            C("ArrestDate", type: "DATETIMEOFFSET"),
            C("ArrestLocation", 128)));

         tables.Add(T("ArrestCharge",
            C("ArrestChargeID", 20, key: true),
            C("ArrestID", 20, fk: "Arrest"),
            C("ChargeSeverityID", 20, fk: "RefChargeSeverity"),
            C("StatuteID", 20, fk: "Statute"),
            C("ChargeDescription", 128),
            C("ChargeStatus", 20)));

         tables.Add(T("Citation",
            C("CitationID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("JurisdictionID", 20, fk: "Jurisdiction"),
            C("CitationNumber", 20),
            C("CitationDate", type: "DATETIMEOFFSET"),
            C("CitationReason", 128)));

         tables.Add(T("CitationCharge",
            C("CitationChargeID", 20, key: true),
            C("CitationID", 20, fk: "Citation"),
            C("StatuteID", 20, fk: "Statute"),
            C("ChargeDescription", 128)));

         tables.Add(T("Warrant",
            C("WarrantID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("IssuingCourtID", 20, fk: "Court"),
            C("WarrantType", 40),
            C("WarrantStatus", 20),
            C("WarrantDate", type: "DATETIMEOFFSET")));

         tables.Add(T("FieldInterview",
            C("InterviewID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("OfficerID", 20, fk: "Employee"),
            C("InterviewDate", type: "DATETIMEOFFSET"),
            C("InterviewLocation", 128),
            C("InterviewNotes", 256)));

         // -- Offenses & case (6) -----------------------------------------
         tables.Add(T("Case",
            C("CaseID", 20, key: true),
            C("IncidentID", 20, fk: "Incident"),
            C("AgencyID", 20, fk: "Agency"),
            C("CaseNumber", 20),
            C("CaseStatus", 20),
            C("OpenedDate", type: "DATETIMEOFFSET")));

         tables.Add(T("CaseCharge",
            C("CaseChargeID", 20, key: true),
            C("CaseID", 20, fk: "Case"),
            C("StatuteID", 20, fk: "Statute"),
            C("ChargeDescription", 128),
            C("Disposition", 60)));

         tables.Add(T("Offense",
            C("OffenseID", 20, key: true),
            C("IncidentID", 20, fk: "Incident"),
            C("StatuteID", 20, fk: "Statute"),
            C("OffenseDate", type: "DATETIMEOFFSET"),
            C("OffenseDescription", 128)));

         tables.Add(T("Statute",
            C("StatuteID", 20, key: true),
            C("StatuteCode", 20),
            C("StatuteCategory", 60),
            C("StatuteDescription", 256)));

         tables.Add(T("Evidence",
            C("EvidenceID", 20, key: true),
            C("CaseID", 20, fk: "Case"),
            C("EvidenceType", 40),
            C("EvidenceDescription", 128),
            C("CollectedDate", type: "DATETIMEOFFSET"),
            C("ChainOfCustody", 128)));

         tables.Add(T("CaseOfficer",
            C("CaseOfficerID", 20, key: true),
            C("CaseID", 20, fk: "Case"),
            C("CaseRole", 40),
            C("AssignedDate", type: "DATETIMEOFFSET")));

         // -- Courts & sentencing (6) -------------------------------------
         tables.Add(T("Court",
            C("CourtID", 20, key: true),
            C("JurisdictionID", 20, fk: "Jurisdiction"),
            C("CourtName", 80),
            C("CourtType", 40)));

         tables.Add(T("CourtAppearance",
            C("AppearanceID", 20, key: true),
            C("CaseID", 20, fk: "Case"),
            C("CourtID", 20, fk: "Court"),
            C("PersonID", 20, fk: "Person"),
            C("AppearanceType", 40),
            C("AppearanceDate", type: "DATETIMEOFFSET"),
            C("AppearanceOutcome", 60)));

         tables.Add(T("Docket",
            C("DocketID", 20, key: true),
            C("CaseID", 20, fk: "Case"),
            C("CourtID", 20, fk: "Court"),
            C("DocketNumber", 20),
            C("DocketStatus", 20)));

         tables.Add(T("Sentence",
            C("SentenceID", 20, key: true),
            C("CaseID", 20, fk: "Case"),
            C("DocketID", 20, fk: "Docket"),
            C("SentenceType", 40),
            C("SentenceStartDate", type: "DATETIMEOFFSET"),
            C("SentenceEndDate", type: "DATETIMEOFFSET"),
            C("DurationMonths", 4, type: "INT")));

         // Deliberate PK-default: ReferencedColumnName omitted so the
         // extractor resolves SentenceCondition.SentenceID -> Sentence PK.
         tables.Add(T("SentenceCondition",
            C("ConditionID", 20, key: true),
            C("SentenceID", 20, fk: "Sentence"),
            C("ConditionType", 40),
            C("ConditionDescription", 128),
            C("ComplianceStatus", 20)));

         tables.Add(T("Parole",
            C("ParoleID", 20, key: true),
            C("PersonID", 20, fk: "Person"),
            C("ParoleStartDate", type: "DATETIMEOFFSET"),
            C("ParoleEndDate", type: "DATETIMEOFFSET"),
            C("ParoleStatus", 20)));

         return tables.ToArray();
      }

      #region -- Builder helpers

      private sealed class Col
      {
         public string Name;
         public int Size = 20;
         public bool Key;
         public string RefTable;
         public string RefColumn;
         public string Type;
      }

      private static Col C(
         string name, int size = 20, bool key = false,
         string fk = null, string fkColumn = null, string type = null)
      {
         return new Col
         {
            Name = name,
            Size = size,
            Key = key,
            RefTable = fk,
            RefColumn = fkColumn,
            Type = type
         };
      }

      private static TableInfo T(string tableName, params Col[] cols)
      {
         var columns = new ColumnList();
         foreach (var col in cols)
         {
            ColumnInfo column = columns.Add(
               SCHEMA, col.Name, col.Size, col.Key, col.Type);

            if (col.Key)
            {
               column.Add(new ConstraintInfo
               {
                  SchemaName = SCHEMA,
                  TableName = tableName,
                  ColumnName = col.Name,
                  Type = DataInfo.PRIMARY_KEY
               });
            }

            if (!String.IsNullOrWhiteSpace(col.RefTable))
            {
               column.Add(new ConstraintInfo
               {
                  SchemaName = SCHEMA,
                  TableName = tableName,
                  ColumnName = col.Name,
                  Type = DataInfo.FOREIGN_KEY,
                  ReferencedTableName = col.RefTable,
                  ReferencedColumnName = col.RefColumn
               });
            }
         }

         return new TableInfo
         {
            SchemaName = SCHEMA,
            TableName = tableName,
            Columns = columns
         };
      }

      #endregion

   }

}
