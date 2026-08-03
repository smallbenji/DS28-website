import axios from "axios";

export default class AuthService {
    public async logout(): Promise<void> {
        await axios({
            url: "/logout",
            method: "GET"
        });
    }
}
