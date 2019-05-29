var mapTypeList = [];
var serverList = [
    { 'id': 1, 'name': 1, 'port': '2000', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' },
    { 'id': 2, 'name': 1, 'port': '2010', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' },
    { 'id': 3, 'name': 1, 'port': '2020', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' },
    { 'id': 4, 'name': 1, 'port': '2030', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' },
    { 'id': 5, 'name': 1, 'port': '2040', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' },
    { 'id': 6, 'name': 1, 'port': '2050', 'ip': 2000, 'type': 'Tank', 'people': '0 / 0' }
];
var serverTypeList = [];

class UserForm extends React.Component {
    postHelper(method, data) {
        var form = new FormData();
        form.append('request', data);
        var requestType = { method: "POST", body: form };
        var url = `admin/${method}`;

        fetch(url, requestType);
    }

    getHelper(method) {
        var requestType = { method: "GET" };
        var url = `admin/${method}`;
        return fetch(url, requestType).then((x) => { return x.json(); });
    }

    constructor(props) {
        super(props);

        this.state = {
            viewModal: false,
            serverList: [],
            viewTankSettings: false
        };

        this.openModal = this.openModal.bind(this);
        this.doTestStart = this.doTestStart.bind(this);
        this.getList = this.getList.bind(this);
        this.getText = this.getText.bind(this);
        this.updateServerList = this.updateServerList.bind(this);
        this.editTankSettings = this.editTankSettings.bind(this);
        this.viewTankSettings = this.viewTankSettings.bind(this);
    }

    getList(method) {
        var list = [];
        this.getHelper(method).then(x => list.push(x[0]));
        return list;
    }

    componentDidMount() {
        mapTypeList = this.getList('GetMapTypes');
        serverTypeList = this.getList('GetServerTypes');
        setInterval(() => this.updateServerList(), 1000);
    }

    updateServerList() {
        let list = this.getList('GetServerList');
        setTimeout(() => this.setState({ serverList: list }), 200);
    }

    getText(div) {
        var result = [];
        div.childNodes.forEach(z => {
            if (z.localName === 'div') {
                if (z.firstChild.localName === 'div') {
                    result.push('' + z.id + ': ' + this.getText(z));
                } else result.push('' + z.id + ': "' + z.lastChild.value + '"');
            }
        });
        return ('{ ' + result.join(', ') + ' }');
    }

    doTestStart() {
        var result = this.getText(document.getElementById('modal'));
        this.openModal();

        this.postHelper("CreateServer", result);
    }

    openModal() {
        var view = !this.state.viewModal;
        this.setState({ viewModal: view, viewTankSettings: view });
    }

    editTankSettings() {
        var id = document.getElementById('ServerId').value;
        console.log(id);
        var result = this.getText(document.getElementById('TankSettings'));
        this.viewTankSettings();

        this.postHelper(`ChangeServerSettings?id=${id}`, result);
    }

    viewTankSettings() {
        this.setState({ viewTankSettings: !this.state.viewTankSettings });
    }

    render() {
        return (
            <div>
                <table className="table">
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Ip</th>
                            <th>Port</th>
                            <th>Type</th>
                            <th>People</th>
                        </tr>
                    </thead>
                    <tbody>{this.state.serverList.map(p => {
                        if (p) 
                        return (
                            <tr key={p.Id}>
                                {Object.keys(p).filter(k => k !== 'Id').map(k => { return (<td><div>{p[k]}</div></td>); })}
                            </tr>
                        );
                    })}</tbody>
                </table>
                <select type="text" defaultValue="0" id="ServerId">{this.state.serverList.map(e => {
                    if (e)
                    return <option value={e.Id}>{e.Name}</option>;
                })}</select>
                <button className={this.state.viewModal ? 'btn btn-primary invisible' : 'btn btn-primary visible'} id="AddServer" onClick={this.viewTankSettings}>{this.state.viewTankSettings ? 'Cancel' : 'Edit'}</button>
                <button className={this.state.viewModal === this.state.viewTankSettings ? 'btn btn-primary visible' : 'btn btn-primary invisible'} id="AddServer" onClick={this.openModal}>{this.state.viewModal ? 'Close' : 'Add'}</button>
                <div id="modal">
                    <div id="SessionName" className={this.state.viewModal ? 'visible' : 'invisible'}>
                        <label>Имя сервера</label>
                        <input type="text" id="Value" />
                    </div>
                    <div id="MapType" className={this.state.viewModal ? 'visible' : 'invisible'}>
                        <label>Тип шаблона карты</label>
                        <select type="text" defaultValue="0" id="Value">{mapTypeList.map(e => {
                            return <option value={e.id}>{e.name}</option>;
                        })}</select>
                    </div>
                    <div id="Width" className={this.state.viewModal ? 'visible' : 'invisible'}>
                        <label>Ширина карты</label>
                        <input type="number" defaultValue="30" id="Value" />
                    </div>
                    <div id="Height" className={this.state.viewModal ? 'visible' : 'invisible'}>
                        <label>Высота карты</label>
                        <input type="number" defaultValue="30" id="Value" />
                    </div>
                    <div id="MaxClientCount" className={this.state.viewModal ? 'visible' : 'invisible'}>
                        <label>Максимальное количество игроков</label>
                        <input type="number" defaultValue="10" id="Value" />
                    </div>
                    <div id="CountOfLifes" className={this.state.viewModal ? 'visible' : 'invisible'}>
                        <label>Количество жизней танков</label>
                        <input type="number" defaultValue="3" id="Value" />
                    </div>
                    <div id="TimeOfInvulnerabilityAfterRespawn" className={this.state.viewModal ? 'visible' : 'invisible'}>
                        <label>Время неуязвимости после перерождения (ms)</label>
                        <input type="number" defaultValue="5000" id="Value" />
                    </div>
                    <div id="MaxCountOfUpgrade" className={this.state.viewModal ? 'visible' : 'invisible'}>
                        <label>Максимальное количество бонусов на карте</label>
                        <input type="number" defaultValue="3" id="Value" />
                    </div>
                    <div id="ServerType" className={this.state.viewModal ? 'visible' : 'invisible'}>
                        <label>Тип сервера</label>
                        <select type="text" defaultValue="0" id="Value">{serverTypeList.map(e => {
                            return <option value={e.id}>{e.name}</option>;
                        })}</select>
                    </div>
                    <div id="TankSettings" className={this.state.viewTankSettings ? 'visible' : 'invisible'}>
                        <div id="StartSesison">
                            <label>Начало игровой сессии</label>
                            <input type="datetime-local" defaultValue="2019-06-01T08:30" id="Value" />
                        </div>
                        <div id="FinishSesison">
                            <label>Конец игровой сессии</label>
                            <input type="datetime-local" defaultValue="2017-06-01T08:40" id="Value" />
                        </div>
                        <div id="GameSpeed">
                            <label>Скорость игры</label>
                            <input type="number" defaultValue="1" id="Value" />
                        </div>
                        <div id="TimeOfActionUpgrades">
                            <label>Время действия бонусов (ms)</label>
                            <input type="number" defaultValue="5000" id="Value" />
                        </div>
                        <div id="ChanceSpawnUpgrades">
                            <label>Шанс появления бонусов</label>
                            <input type="number" defaultValue="0.995" id="Value" />
                        </div>
                        <div id="TankSpeed">
                            <label>Множитель скорости танка</label>
                            <input type="number" defaultValue="2" id="Value" />
                        </div>
                        <div id="BulletSpeed">
                            <label>Множитель скорости пули</label>
                            <input type="number" defaultValue="4" id="Value" />
                        </div>
                        <div id="TankDamage">
                            <label>Урон танка</label>
                            <input type="number" defaultValue="40" id="Value" />
                        </div>
                        <div id="TankMaxHP">
                            <label>Максимальный HP танков</label>
                            <input type="number" defaultValue="100" id="Value" />
                        </div>
                        <div id="IncreaseBulletSpeed">
                            <label>Показатели бонуса увеличения скорости пуль</label>
                            <input type="number" defaultValue="2" id="Value" />
                        </div>
                        <div id="IncreaseDamage">
                            <label>Показатели бонуса увеличения урона танков</label>
                            <input type="number" defaultValue="20" id="Value" />
                        </div>
                        <div id="RestHP">
                            <label>Показатели бонуса лечения</label>
                            <input type="number" defaultValue="25" id="Value" />
                        </div>
                        <div id="TimeOfInvulnerability">
                            <label>Показатели бонуса неуязвимости (ms)</label>
                            <input type="number" defaultValue="5000" id="Value" />
                        </div>
                        <div id="IncreaseHP">
                            <label>Показатели бонуса увеличения максимального количества HP</label>
                            <input type="number" defaultValue="125" id="Value" />
                        </div>
                        <div id="IncreaseSpeed">
                            <label>Показатели бонуса увеличения скорости танка</label>
                            <input type="number" defaultValue="1" id="Value" />
                        </div>
                        <button className={this.state.viewTankSettings !== this.state.viewModal ? 'btn btn-primary visible' : 'btn btn-primary invisible'} onClick={this.editTankSettings}>Change</button>
                    </div>
                    <button className={this.state.viewModal ? 'btn btn-primary visible' : 'btn btn-primary invisible'} onClick={this.doTestStart}>Create</button>
                </div>
            </div >
        );
    }
}
ReactDOM.render(
    <UserForm />,
    document.getElementById("app")
)