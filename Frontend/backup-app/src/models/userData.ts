export interface PathData {
    sourcePaths: string[];
    destinationPaths: string[];
  }
  export interface Phone {
    paths: PathData;
  }
  export interface HDD {
    paths: PathData;
    deviceLetter: string;
  }
  export interface SD {
    paths: PathData;
    deviceLetter: string;
  }
  export interface UserData {
    phone: Phone;
    hdd: HDD;
    sd: SD
  }